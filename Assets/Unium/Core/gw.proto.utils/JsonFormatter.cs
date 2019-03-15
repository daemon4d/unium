using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gw.proto.utils
{
    public class JsonFormatter
    {
        public static string ResponseMessage(string id, string msg, string data)
        {
            var sb = new StringBuilder();

            sb.Append("{");

            if (String.IsNullOrEmpty(id) == false)
            {
                sb.AppendFormat(@"""id"":{0},", JsonTypeConverters.EscapedString(id));
            }

            sb.AppendFormat(@"""{0}"":{1}", msg, data);

            sb.Append("}");
            return sb.ToString();
        }
    }
}
