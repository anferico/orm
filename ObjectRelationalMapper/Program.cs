using System;
using CodeGeneration;
using System.Data.SQLite;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace ObjectRelationalMapper
{
	class Program
	{
		public static void Main(string[] args)
		{
			string createTablePublisher = File.ReadAllText(
                "../CodeGeneration/publisher.sql"
            );
			
            string createTableBook = File.ReadAllText(
                "../CodeGeneration/book.sql"
            );

			SQLiteConnection.CreateFile("mydb.sqlite");
			var conn = new SQLiteConnection("Data Source=mydb.sqlite;Version=3;");

			conn.Open();
			new SQLiteCommand(createTablePublisher, conn).ExecuteNonQuery();
			new SQLiteCommand(createTableBook, conn).ExecuteNonQuery();
			conn.Close();

			var tableAnnotations = new Parser().Parse("annotated_interfaces");
            
			conn.Open();
			var bookManager = new EntityManager<Book>(tableAnnotations, conn);
			var pubManager = new EntityManager<Publisher>(tableAnnotations, conn);

			var p1 = new Publisher() { id = 0, name = "Einaudi" };
			var p2 = new Publisher() { id = 1, name = "Rizzoli" };
			var b1 = new Book() { id = 0, title = "Gabbiano", publisher = p1};
			var b2 = new Book() { id = 1, title = "Livingston", publisher = p1 };
			var b3 = new Book() { id = 2, title = "Guida galattica", publisher = p2 };
			var b4 = new Book() { id = 3, title = "1984", publisher = p2 };
			p1.books = new List<Book>() { b1, b2 };
			p2.books = new List<Book>() { b3, b4 };

			pubManager.persist(p1);
			pubManager.persist(p2);
			var query = bookManager.createQuery(
				"SELECT id FROM book WHERE publisher > 0;"
			);
			var book = query.getResultList();

			// var p = new Publisher() { id = 0, name = "Einaudi" };
			// var b = new Book() { id = 0, title = "1Q84", publisher = p };
			// p.books = new List<Book>() { b };
			// bookManager.persist(b);
			// pubManager.persist(p);
			// var whatever1 = bookManager.find(b.id);
			// var whatever2 = pubManager.find(p.id);
			// bookManager.remove(b);
			// pubManager.remove(p);

			conn.Close();
		}
	}
}