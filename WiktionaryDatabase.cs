//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34011
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MySql.Data.MySqlClient;

namespace beastie
{
	public class WiktionaryDatabase
	{
		public WiktionaryDatabase ()
		{
		}

		//warning: fails due to memory running out.
		//TODO: replace with something like this: https://stackoverflow.com/questions/13648523/how-to-import-large-sql-file-using-mysql-exe-through-streamreader-standardinp
		public static void ImportDatabaseFile(string filename, bool compressed = true) {
			CatalogueOfLifeDatabase.CreateWiktionaryDatabase();

			using (MySqlConnection connection = CatalogueOfLifeDatabase.Connection()) {
				//connection.Open();
				using (MySqlCommand command = new MySqlCommand()) {
					command.Connection = connection;
					using(MySqlBackup mb = new MySqlBackup(command)) {
						StreamReader reader;
						if (compressed || filename.EndsWith(".gz")) {
							GZipStream stream = new GZipStream(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read), CompressionMode.Decompress);
							reader = new StreamReader(stream, Encoding.Unicode); //utf8 causes duplicate entry errors
						} else {
							reader = new StreamReader(filename, Encoding.Unicode);
						}
						command.CommandTimeout = 900;
						connection.Open();
						//mb.ImportFromTextReader(new StringReader("USING );
						mb.ImportFromTextReader(reader);
						connection.Close();
						reader.Close();
						//command.ExecuteReader();

						//MySqlBulkLoader loader = new MySqlBulkLoader(connection);
						//loader.
					}
				}
			}

		}

		public static void Stuff() {
			//define( 'NS_MAIN', 0 );
			//define( 'NS_CATEGORY', 14 );
			//define( 'NS_TEMPLATE', 10 );

			// all pages in category:
			//SELECT convert(page.page_title using utf8) as page_title, convert(cl_to using utf8) as cl_to, categorylinks.* FROM enwiktionary.categorylinks JOIN page ON cl_from = page_id where cl_to = 'Icelandic_words_prefixed_with_af-' ;
			//SELECT convert(page.page_title using utf8) as page_title, convert(cl_to using utf8) as cl_to, categorylinks.* FROM enwiktionary.categorylinks JOIN page ON cl_from = page_id where cl_to = 'English_nouns';

			//number of categories: USE enwiktionary;
			// SELECT count(*) FROM category; -- 183081
			// SELECT count(DISTINCT cl_to) FROM categorylinks; -- 114540
			// SELECT count(DISTINCT page_id) from page where page_namespace = 14; -- 110204 -- category pages
			// SELECT count(DISTINCT cl_from) FROM categorylinks JOIN page on page_id = cl_from WHERE page_namespace = 14; -- 110179 (categorized categories)

			// categorized pages:
			// SELECT count(DISTINCT cl_from) FROM enwiktionary.categorylinks; -- 3826452

			// language categories:
			// SELECT count(*) FROM category WHERE cat_title LIKE "%language" or cat_title LIKE "%Language" 
			// SELECT convert(cat_title using utf8) FROM category WHERE cat_title LIKE "%language" or cat_title LIKE "%Language"  // includes, e.g. Entry_templates_by_language

			// all language categories under "Category:All_languages" 
		


			string catInCat = 
				@"SELECT convert(page.page_title using utf8) as page_title
				FROM enwiktionary.categorylinks 
					JOIN page ON cl_from = page_id 
					where cl_to = 'English_nouns'
					AND page_namespace = 14;";
		}

		static string query_langauageCats = @"
				USE enwiktionary;
				SELECT page_title
				-- SELECT convert(page_title using utf8), page_is_redirect, page_namespace 
				FROM 
					categorylinks
					JOIN page ON (cl_from = page_id)
				WHERE 
					cl_to = ?cat_title
					AND cl_type = 'subcat'
					AND page_namespace = 14; -- redundant / same as above";
		//-- AND (page_title LIKE '%language' or page_title LIKE '%Language')

		public static void BuildLanguageCategoryTable() {


			// 1544 results ending in "Language" or "language".. list could have also been made by finding Pages that transclude to "Template:langcatboiler"

			Dictionary<byte[], string> cats = FindSubcats();
			foreach (byte[] cat in cats.Keys) {
				Console.WriteLine("{0} => {1}", cats[cat], TitleToString(cat));
			}

		}

		static Dictionary<byte[], string> FindSubcats(byte[] category = null, Dictionary<byte[], string> categoryToCode = null, string code = null) {
			WiktionaryData wiktionaryData = WiktionaryData.Instance();

			bool isFirstRun = false;
			if (categoryToCode == null) {
				categoryToCode = new Dictionary<byte[], string>(new ByteArrayComparer());
				isFirstRun = true;
			}

			using (MySqlConnection connection = CatalogueOfLifeDatabase.Connection()) 
			using (MySqlCommand command = connection.CreateCommand()) {
				connection.Open();
				command.CommandText = query_langauageCats;
				if (category != null) {
					command.Parameters.AddWithValue("cat_title", category);
				} else if (isFirstRun) {
					command.Parameters.AddWithValue("cat_title", "All_languages");
				}
				MySqlDataReader rdr = command.ExecuteReader();

				while (rdr.Read()) {
					byte[] subcat = (byte[]) rdr[0];
					string title = TitleToString(subcat);

					if (subcat == null || subcat.Length == 0 || title == "" || title == "0") continue;

					if (isFirstRun) {
						if (wiktionaryData.catnameIndex.ContainsKey(title)) {
							Language lang = wiktionaryData.catnameIndex[title];
							code = lang.code;
						} else {
							if (!title.EndsWith("language", StringComparison.OrdinalIgnoreCase)) {
								Console.WriteLine("Language category not found in Wiktionary Data: {0}", title);
							}
							continue;

							/* non language cats:
							Bulu language =>  Category:Bulu (Cameroon) language
							Cheq Wong language => Category:Chewong language.
							Comorian language => Category:Maore Comorian language
							Lebanese Arabic language
							Leti language => Category:Leti (Indonesia) language
							Regional terms by language */
						}
					}

					if (! categoryToCode.ContainsKey(subcat)) {
						Console.WriteLine("Added Category: {0} => {1}", code, title);
						categoryToCode[subcat] = code;
						categoryToCode = FindSubcats(subcat, categoryToCode, code);

					} else if (categoryToCode[subcat] != code) {
						Console.WriteLine("Category ({{0}}) belongs to multiple languages: {1}, {2}", title, categoryToCode[subcat], code);
						categoryToCode[subcat] = "ambiguous";
					}

				}
				rdr.Close();
			}

			return categoryToCode;
		}

		static string TitleToString(byte[] bytes) {
			//return System.Text.Encoding.UTF8.GetString(bytes);
			return System.Text.Encoding.UTF8.GetString(bytes).Replace("_", " ");
		}
	}


	public class ByteArrayComparer : IEqualityComparer<byte[]> {
		public bool Equals(byte[] left, byte[] right) {
			if ( left == null || right == null ) {
				return left == right;
			}
			return left.SequenceEqual(right);
		}
		public int GetHashCode(byte[] key) {
			if (key == null)
				throw new ArgumentNullException("key");
			return key.Sum(b => b);
		}
	}

}

