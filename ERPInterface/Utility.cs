using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Serialization;
using System.Runtime.Serialization.Json;

namespace ERPInterface
{
    public static class Utility
    {
        public static string XmlSerializeToString(Object objectInstance)
        {          
            var serializer = new XmlSerializer(objectInstance.GetType());
            var sb = new StringBuilder();

            using (TextWriter writer = new StringWriter(sb))
            {
                serializer.Serialize(writer, objectInstance);
            }

            return sb.ToString();
        }

        public static object XmlDeserializeFromString(this string objectData, Type type)
        {
            var serializer = new XmlSerializer(type);
            object result;

            using (TextReader reader = new StringReader(objectData))
            {
                result = serializer.Deserialize(reader);
            }

            return result;
        }

        public static string ObjectToJson(object obj)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
            MemoryStream stream = new MemoryStream();
            serializer.WriteObject(stream, obj);
            byte[] dataBytes = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(dataBytes, 0, (int)stream.Length);
            return Encoding.UTF8.GetString(dataBytes);
        }

        public static object JsonToObject(string jsonString, object obj)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
            MemoryStream mStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
            return serializer.ReadObject(mStream);
        }

        public static string XmlResult(string ret)
        {
            Result result = new Result();
            result.successful = ret == "";
            result.erroMsg = ret;
            return Utility.XmlSerializeToString(result);
        }

        public static string XmlResult(string ret,string msg)
        {
            Result result = new Result();
            result.successful = ret == "";
            result.erroMsg = result.successful ? msg : ret;
            return Utility.XmlSerializeToString(result);
        }
    }

    public class Result
    {
        public bool successful { get; set; }
        public string erroMsg { get; set; }
    }
}