using System;

namespace CodeGeneration 
{
    public class MemberAnnotation : Annotation 
    {
        public string FieldType;
        public string FieldName;

        public MemberAnnotation(
            string annotationName, string fieldType, string fieldName
        ) : base(annotationName) {
            FieldType = fieldType;
            FieldName = fieldName;
        }
    }
}
