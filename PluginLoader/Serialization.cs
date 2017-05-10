using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace Rynchodon.PluginLoader
{
	public static class Serialization
	{

		public static void ReadJson<T>(string filePath, out T obj)
		{
			DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
			using (var file = File.Open(filePath, FileMode.Open))
			using (var reader = JsonReaderWriterFactory.CreateJsonReader(file, XmlDictionaryReaderQuotas.Max))
				obj = (T)serializer.ReadObject(reader);
		}

		public static void WriteJson<T>(string filePath, T obj, bool overwrite = false)
		{
			DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
			using (var file = File.Open(filePath, overwrite ? FileMode.Create : FileMode.CreateNew))
			using (var writer = JsonReaderWriterFactory.CreateJsonWriter(file, Encoding.UTF8, false, true))
				serializer.WriteObject(writer, obj);
		}

		public static void WriteXml<T>(string filePath, T obj, bool overwrite = false)
		{
			DataContractSerializer serializer = new DataContractSerializer(typeof(T));
			using (var file = File.Open(filePath, overwrite ? FileMode.Create : FileMode.CreateNew))
			using (var writer = new XmlTextWriter(file, Encoding.UTF8) { Formatting = Formatting.Indented })
				serializer.WriteObject(writer, obj);
		}

		public static void ReadXml<T>(string filePath, out T obj)
		{
			DataContractSerializer serializer = new DataContractSerializer(typeof(T));
			using (var file = File.Open(filePath, FileMode.Open))
			using (var reader = new XmlTextReader(file))
				obj = (T)serializer.ReadObject(reader);
		}

	}
}
