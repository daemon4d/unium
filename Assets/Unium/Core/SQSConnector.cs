using UnityEngine;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon;
using Debug = UnityEngine.Debug;
using TinyJson;
using System.Collections.Generic;
using gw.proto.http;

namespace gw.unium
{
    public class SQSConnector : MonoBehaviour
    {
        public string IncomingQueueUrl = null;
        public string OutgoingQueueUrl = null;

        private AmazonSQSClient mSQSClient;
        private List<SQSMessage> mMessageQueue = new List<SQSMessage>();

        void Awake()
        {
            UnityInitializer.AttachToGameObject(this.gameObject);
        }

        private void OnEnable()
        {
            var awsCredentials = new BasicAWSCredentials("AKIAJTGVB3QMP2EDT33A", "B0rLNGY5yahhzRfF6EbIjJxDNOsEIU2s+/7i2vJN");
            Amazon.AWSConfigs.HttpClient = Amazon.AWSConfigs.HttpClientOption.UnityWebRequest;
            mSQSClient = new AmazonSQSClient(awsCredentials, Amazon.RegionEndpoint.USEast1);
            StartCoroutine(CheckNewSQSMessages(1.0f));
        }

        private System.Collections.IEnumerator CheckNewSQSMessages(float waitTime)
        {
            while (true)
            {
                yield return new WaitForSeconds(waitTime);
                if (!string.IsNullOrEmpty(IncomingQueueUrl))
                {
                    mSQSClient.ReceiveMessageAsync(IncomingQueueUrl, (result) =>
                    {
                        if (result.Exception == null)
                        {
                            var messages = result.Response.Messages;
                            Debug.Log("Got " + messages.Count + " messages");
                            List<SQSMessage> newMessages = new List<SQSMessage>();
                            List<DeleteMessageBatchRequestEntry> deleteReq = new List<DeleteMessageBatchRequestEntry>();
                            messages.ForEach(m =>
                            {
                                var msg = m.Body.FromJson<SQSMessage>();
                                msg.id = m.MessageId;
                                newMessages.Add(msg);
                                deleteReq.Add(new DeleteMessageBatchRequestEntry(m.MessageId, m.ReceiptHandle));
                            });
                            if (newMessages.Count>0)
                            { 
                                lock (mMessageQueue)
                                {
                                    mMessageQueue.AddRange(newMessages);
                                }
                                mSQSClient.DeleteMessageBatchAsync(IncomingQueueUrl, deleteReq, (delResult) =>
                                {
                                    if (delResult.Exception != null)
                                    {
                                        Debug.LogException(delResult.Exception);
                                    }
                                });
                            }
                        }
                        else
                        {
                            Debug.LogException(result.Exception);
                        }
                    });
                }
                else
                {
                    Debug.Log(@"Queue Url is empty, make sure that the queue is created first");
                }
            }
        }

        void LateUpdate()
        {
            Tick();    
        }

        private void Tick()
        {
            lock (mMessageQueue)
            {
                if (mMessageQueue.Count == 0)
                {
                    return;
                }

                // dispatch one message per frame

                var msg = mMessageQueue[0];
                mMessageQueue.RemoveAt(0);
                
                var req = new RequestAdapterSQS(msg, mSQSClient, OutgoingQueueUrl);
                // find route

                var route = Unium.RoutesHTTP.Find(msg.q);

                if (route == null || route.Handler == null)
                {
                    req.Error(ResponseCode.NotFound);
                    return;
                }

                // queue for repetition?

                if (msg.repeat != null)
                {
                    //route.SetCacheContext(req, msg.repeat.cache ? 1 : 0);
                    //mRepeaters.Add(new Repeater(route, req));
                    UnityEngine.Debug.Log("Repeating not implemented yet");
                }

                // otherwise just dispatch

                else
                {
                    route.Dispatch(req);
                }
            }
        }
    }
}
