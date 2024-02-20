using System.Text.Json;
using System.Text.Json.Nodes;

namespace CSX
{
    public interface ICSX
    {
        void Start(string IPAdress, string Port);

        void Stop();

        void AnswerRequest(string Method);

        string Post(JsonObject json);

        void Put(string CSXKEY, JsonObject json);

        JsonObject Get(JsonObject json);

        JsonObject GetByUCID(JsonObject json);

        void ClearData(object sender, System.Timers.ElapsedEventArgs e);
    }
}