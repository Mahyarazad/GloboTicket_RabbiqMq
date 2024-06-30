using Newtonsoft.Json;
using System.Text;

namespace GloboTicket.Integration
{
    public static class Serializer<T> where T : class
    {
        public static byte[] Serialize(T input)
        {
            if (input is null)
            {
                return null!;
            }

            var serialized = JsonConvert.SerializeObject(input); 
            return Encoding.UTF8.GetBytes(serialized);
        }

        public static T Deserialize(byte[] byteArr)
        {
            var input = Encoding.UTF8.GetString(byteArr);
            return JsonConvert.DeserializeObject<T>(input);
        }
    }
}
