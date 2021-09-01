using System.IO;
using System.Text.Json;

namespace CSharpGuide.serializes
{
    public interface IProxyRequest
    {
        void WriteJson(Utf8JsonWriter writer);
    }

    public class IndexRequest<TDocument> : IProxyRequest
    {
        public TDocument? Document { get; set; }

        public void WriteJson(Utf8JsonWriter writer)
        {
            if (Document is null) return;


        }
    }
}
