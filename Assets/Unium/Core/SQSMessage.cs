using UnityEngine;
using Amazon.Runtime;

namespace gw.unium
{

    public class SQSMessage
    {
        // from json

        public class Repeat
        {
            public int skip = 0;
            public int samples = int.MaxValue;
            public float freq = 1.0f;
            public bool cache = false;
        }

        public string id;             // arbitary message identifier
        public string q;              // query
        public Repeat repeat;
    }
}
