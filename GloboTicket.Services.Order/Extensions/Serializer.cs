using Newtonsoft.Json;
using System.Text;

namespace GloboTicket.Services.Ordering.Extensions
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
            return Encoding.ASCII.GetBytes(serialized);
        }

        public static T Deserialize(byte[] byteArr)
        {
            var input = Encoding.ASCII.GetString(byteArr);
            return JsonConvert.DeserializeObject<T>(input);
        }
    }
}
