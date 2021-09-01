using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CSharpGuide.serializes
{
    public class JsonDocumentOps
    {
        const string jsonBody = @"
{
  'Class Name': 'Science',
  'Teacher\u0027s Name': 'Jane',
  'Semester': '2019-01-01',
  'Students': [
    {
      'Name': 'John',
      'Grade': 94.3
    },
    {
      'Name': 'James',
      'Grade': 81.0
    },
    {
    'Name': 'Julia',
      'Grade': 91.9
    },
    {
    'Name': 'Jessica',
      'Grade': 72.4
    },
    {
    'Name': 'Johnathan'
    }
  ],
  'Final': true
}
";

        void ReadJson()
        {
            double sum = 0;
            int count = 0;

            using var document = JsonDocument.Parse(jsonBody);
            JsonElement root = document.RootElement;
            JsonElement studentsElement = root.GetProperty("Students");

            count = studentsElement.GetArrayLength();   // 可以快速获取数量，不用循环+1

            foreach (JsonElement student in studentsElement.EnumerateArray())
            {
                if (student.TryGetProperty("Grade", out JsonElement gradeElement))
                {
                    sum += gradeElement.GetDouble();
                }
                else
                {
                    sum += 70;
                }
                //count++;
            }
            double average = sum / count;
            Console.WriteLine($"Average grade : {average}");
        }

        void WriteJson()
        {
            //string jsonString = File.ReadAllText(inputFileName);
            string jsonString = jsonBody;
            var writerOptions = new JsonWriterOptions
            {
                Indented = true
            };

            var documentOptions = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip
            };

            using MemoryStream ms = new();
            using var writer = new Utf8JsonWriter(ms, options: writerOptions);
            using JsonDocument document = JsonDocument.Parse(jsonString, documentOptions);

            JsonElement root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                writer.WriteStartObject();
            }
            else
            {
                return;
            }
            foreach (JsonProperty property in root.EnumerateObject())
            {
                property.WriteTo(writer);
            }

            writer.WriteEndObject();

            writer.Flush();
        }
    }
}
