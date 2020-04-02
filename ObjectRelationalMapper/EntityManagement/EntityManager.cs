using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;
using CodeGeneration;
using System.Reflection;
using System.Data;
using System.Collections;

namespace ObjectRelationalMapper 
{
    public class EntityManager<T> : IEntityManager<T> where T : new() 
    {
        private List<TableAnnotation> tableAnnotations;
        private QueryGenerator queryGenerator;
        private SQLiteConnection dbConnection;

        public EntityManager(
            List<TableAnnotation> tableAnnotations, 
            SQLiteConnection dbConnection
        ) {
            this.tableAnnotations = tableAnnotations;
            this.queryGenerator = new QueryGenerator();
            this.dbConnection = dbConnection;
        }

        public void persist(T entity) 
        {
            persistPrivate(entity, null, null);
        }

        private void persistPrivate(T entity, string startingClass, object fkeyValue) 
        {
            var entityType = typeof(T);
            var tabAnn = tableAnnotations.Find(ann => 
                ann.InterfaceName == entityType.Name
            );

            string tableName = tabAnn.Attributes["name"];
            var membersValues = new List<object>();
            foreach (var membAnn in tabAnn.MemberAnnotations) 
            {
                object memberValue = entityType.GetFields().First(field =>
                    field.Name == membAnn.FieldName
                ).GetValue(entity);

                switch (membAnn.AnnotationName) 
                {
                    case "Id":
                    case "Column":
                        membersValues.Add(memberValue);
                        break;

                    case "Many2One":
                        string referencedClass = membAnn.Attributes["target"];
                        if (startingClass != referencedClass) 
                        {
                            var referencedField = tableAnnotations.Find(ann =>
                                ann.InterfaceName == referencedClass
                              ).MemberAnnotations.Find(ann =>
                                ann.AnnotationName == "Id"
                            ).FieldName;

                            membersValues.Add(
                                memberValue.GetType().GetField(referencedField).GetValue(memberValue)
                            );
                            var manager = Activator.CreateInstance(
                                typeof(EntityManager<>).MakeGenericType(
                                    memberValue.GetType()
                                ),
                                new object[] { tableAnnotations, dbConnection }
                            );

                            manager.GetType().GetMethod(
                                "persistPrivate", BindingFlags.NonPublic | BindingFlags.Instance
                            ).Invoke(manager, new object[] { memberValue, null, null });
                        }
                        else
                        {
                            membersValues.Add(fkeyValue);
                        }
                        break;

                    case "One2Many":
                        var idFieldName = tabAnn.MemberAnnotations.Find(ann =>
                            ann.AnnotationName == "Id"
                        ).FieldName;
                        var idFieldValue = entityType.GetField(idFieldName).GetValue(entity);
                        var listFieldValue = (IEnumerable)entityType.GetField(membAnn.FieldName).GetValue(entity);
                        var type = Type.GetType(
                            $"CodeGeneration.{membAnn.Attributes["target"]}, CodeGeneration"
                        );
                        var mng = Activator.CreateInstance(
                            typeof(EntityManager<>).MakeGenericType(type),
                            new object[] { tableAnnotations, dbConnection }
                        );

                        foreach (var obj in listFieldValue) 
                        {
                            mng.GetType().GetMethod(
                                "persistPrivate", BindingFlags.NonPublic | BindingFlags.Instance
                            ).Invoke(mng, new object[] { obj, tabAnn.InterfaceName, idFieldValue });
                        }

                        break;
                }
            }

            string query = queryGenerator.CreateInsertQuery(
                tableName, 
                membersValues
            );
            new SQLiteCommand(query, dbConnection).ExecuteNonQuery();
        }

        public void remove(T entity) 
        {
            var entityType = typeof(T);
            var tabAnn = tableAnnotations.Find(ann => 
                ann.InterfaceName == entityType.Name
            );

            string tableName = tabAnn.Attributes["name"];
            string pkeyName = tabAnn.MemberAnnotations.Find(membAnn =>
                membAnn.AnnotationName == "Id"
            ).Attributes["name"];
            object pkeyValue = entityType.GetField(pkeyName).GetValue(entity);

            string deleteQuery = queryGenerator.CreateDeleteQuery(
                tableName, 
                pkeyName, 
                pkeyValue
            );
            new SQLiteCommand(deleteQuery, dbConnection).ExecuteNonQuery();
        }

        public T find(object primaryKey) 
        {
            return findPrivate(primaryKey, null, null);
        }

        private T findPrivate(object primaryKey, string startingClass, object linkedObj) 
        {
            var deserializedObjType = typeof(T);
            var tabAnn = tableAnnotations.Find(ann => 
                ann.InterfaceName == deserializedObjType.Name
            );
            string tableName = tabAnn.Attributes["name"];
            string pkeyName = tabAnn.MemberAnnotations.Find(membAnn =>
                membAnn.AnnotationName == "Id"
            ).Attributes["name"];

            string query = queryGenerator.CreateSelectQuery(
                tableName, 
                pkeyName, 
                primaryKey
            );
            var reader = new SQLiteCommand(query, dbConnection).ExecuteReader();
            reader.Read();
            T deserializedObj = new T();
            foreach (var membAnn in tabAnn.MemberAnnotations) 
            {
                switch (membAnn.AnnotationName) 
                {
                    case "Id":
                    case "Column":
                        deserializedObjType.GetField(membAnn.FieldName).SetValue(
                            deserializedObj, reader[membAnn.Attributes["name"]]
                        );
                        break;

                    case "Many2One":
                        object referencedObj;
                        if (startingClass == membAnn.Attributes["target"]) 
                        {
                            referencedObj = linkedObj;
                        }
                        else 
                        {
                            object fkeyValue = reader[membAnn.Attributes["name"]];

                            var type = Type.GetType(
                                $"CodeGeneration.{membAnn.Attributes["target"]}, CodeGeneration"
                            );
                            var manager = Activator.CreateInstance(
                                typeof(EntityManager<>).MakeGenericType(type),
                                new object[] { tableAnnotations, dbConnection }
                            );
                            referencedObj = manager.GetType().GetMethod(
                                "findPrivate", BindingFlags.NonPublic | BindingFlags.Instance
                            ).Invoke(manager, new object[] { fkeyValue, null, null });
                        }

                        deserializedObjType.GetField(membAnn.FieldName).SetValue(
                            deserializedObj, 
                            referencedObj
                        );

                        break;

                    case "One2Many":
                        var mappingTabAnn = tableAnnotations.Find(ann =>
                            ann.InterfaceName == membAnn.Attributes["target"]
                        );
                        string mappingAttrName = membAnn.Attributes["mappedBy"];

                        var idMembAnn = tabAnn.MemberAnnotations.Find(ann =>
                            ann.AnnotationName == "Id"
                        );
                        object idFieldValue = deserializedObjType.GetField(
                            idMembAnn.FieldName
                        ).GetValue(deserializedObj);

                        string selectQuery = queryGenerator.CreateSelectQuery(
                            mappingTabAnn.Attributes["name"], 
                            mappingAttrName, 
                            idFieldValue
                        );

                        var rdr = new SQLiteCommand(
                            selectQuery, 
                            dbConnection
                        ).ExecuteReader();
                        var t = Type.GetType(
                            $"CodeGeneration.{membAnn.Attributes["target"]}, CodeGeneration"
                        );
                        var mng = Activator.CreateInstance(
                            typeof(EntityManager<>).MakeGenericType(t),
                            new object[] { tableAnnotations, dbConnection }
                        );
                        var relatedObjs = Activator.CreateInstance(
                            typeof(List<>).MakeGenericType(t), null
                        );

                        while (rdr.Read()) 
                        {
                            object relatedObj = mng.GetType().GetMethod(
                                "findPrivate", BindingFlags.NonPublic | BindingFlags.Instance
                            ).Invoke(mng, new object[] { 
                                rdr[idMembAnn.Attributes["name"]], 
                                tabAnn.InterfaceName, 
                                deserializedObj 
                            });

                            relatedObjs.GetType().GetMethod("Add").Invoke(
                                relatedObjs, new object[] { relatedObj }
                            );
                        }

                        deserializedObjType.GetField(membAnn.FieldName).SetValue(
                            deserializedObj, 
                            relatedObjs
                        );

                        break;
                }
            }
            return deserializedObj;
        }

        public Query<T> createQuery(string query) 
        {
            return new Query<T>(this, new SQLiteCommand(query, dbConnection));
        }
    }
}
