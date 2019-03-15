// Copyright (c) 2017 Gwaredd Mountain, https://opensource.org/licenses/MIT

using System;
using gw.proto.http;
using UnityEngine;
using Amazon.Runtime;
using Amazon.SQS;
using gw.proto.utils;

namespace gw.unium
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public abstract class RequestAdapter
    {
        public virtual  string  Path                                { get { return null; } }
        public virtual  string  Query                               { get { return null; } }
        public virtual  byte[]  Body                                { get { return null; } }

        public abstract void Reject( ResponseCode code );
        public abstract void Redirect( string url );
        public abstract void Respond( string data );
        public abstract void Respond( byte[] data );

        public abstract void SetContentType( string mimetype );

        public object CachedContext = null;
    }

#if !UNIUM_DISABLE && ( DEVELOPMENT_BUILD || UNITY_EDITOR || UNIUM_ENABLE )

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    // restful adapter

    public class RequestAdapterHTTP : RequestAdapter
    {
        HttpRequest mRequest;
        string      mPath;

        public RequestAdapterHTTP( HttpRequest req )                { mRequest = req; mPath = WWW.UnEscapeURL( req.URL ); }

        public override String  Path                                { get { return mPath; } }
        public override String  Query                               { get { return mRequest.QueryParameters; } }
        public override byte[]  Body                                { get { return mRequest.Method == "POST" ? mRequest.Body : null; } }

        public override void Reject( ResponseCode code )            { mRequest.Reject( code ); }
        public override void Redirect( string url )                 { mRequest.Redirect( url ); }
        public override void Respond( string data )                 { mRequest.Send( data ); }
        public override void Respond( byte[] data )                 { mRequest.Send( data ); }

        public override void SetContentType( string mimetype )      { mRequest.Response.Headers[ "Content-Type" ] = mimetype; }
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////
    // socket adapter

    public class RequestAdapterSocket : RequestAdapter
    {
        UniumSocket.Message mMessage;

        public RequestAdapterSocket( UniumSocket.Message msg )
        {
            mMessage = msg;
        }

        public string               ID                          { get { return mMessage.id; } }
        public UniumSocket.Message  Message                     { get { return mMessage; } }
        public UniumSocket          Socket                      { get { return mMessage.Socket; } }
        public bool                 Rejected                    { get; private set; }


        public override String Path                             { get { return mMessage.q; } }
        public override byte[] Body                             { get { return null; } }

        public override void SetContentType( string mimetype )  {}

        public override void Reject( ResponseCode code )        { mMessage.Error( code ); Rejected = true; }
        public override void Redirect( string url )             { mMessage.Error( ResponseCode.MovedPermanently ); Rejected = true; }
        public override void Respond( string data )             { mMessage.Reply( data ); }
        public override void Respond( byte[] data )             { throw new NotImplementedException(); } // binary data

        public void Info( string msg )                          { mMessage.Info( msg ); }
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////
    // memory adapter

    public class RequestAdapterMemory : RequestAdapter
    {
        public RequestAdapterMemory( string path )              { mPath = path; }

        public override String Path                             { get { return mPath; } }

        public override void Reject( ResponseCode code )        { mCode = code; }
        public override void Redirect( string url )             { mCode = ResponseCode.MovedPermanently; }
        public override void Respond( string data )             { mResult = data; }
        public override void Respond( byte[] data )             {}

        public override void SetContentType( string mimetype )  {}

        string       mPath      = "";
        string       mResult    = null;
        ResponseCode mCode      = ResponseCode.OK;

        public bool     IsError                                 { get { return mCode != ResponseCode.OK || mResult == null; } }
        public string   Data                                    { get { return mResult; } }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    // socket adapter

    public class RequestAdapterSQS : RequestAdapter
    {
        private SQSMessage mMessage;
        private AmazonSQSClient mSQSClient;
        private string mOutboudQueue; 

        public RequestAdapterSQS(SQSMessage msg, AmazonSQSClient sqsClient, string outboundQueue)
        {
            mMessage = msg;
            mSQSClient = sqsClient;
            mOutboudQueue = outboundQueue;
        }

        public string ID { get { return mMessage.id; } }
        public bool Rejected { get; private set; }


        public override String Path { get { return mMessage.q; } }
        public override byte[] Body { get { return null; } }

        public override void SetContentType(string mimetype) { }

        public override void Reject(ResponseCode code) { Error(code); Rejected = true; }
        public override void Redirect(string url) { Error(ResponseCode.MovedPermanently); Rejected = true; }
        public override void Respond(string data) { Reply(data); }
        public override void Respond(byte[] data) { throw new NotImplementedException(); } // binary data

        private void Reply(string data) { SendImpl(ID, "data", string.IsNullOrEmpty(data) ? "null" : data); }
        private void Reply(object data) { SendImpl(ID, "data", JsonReflector.Reflect(data)); }
        public void Info(string msg) { SendImpl(ID, "info", JsonTypeConverters.EscapedString(msg)); }
        public void Error(ResponseCode code) { SendImpl(ID, "error", JsonTypeConverters.EscapedString(HttpUtils.CodeToString(code))); }

        private void SendImpl(string id, string msg, string data) {
            mSQSClient.SendMessageAsync(mOutboudQueue, JsonFormatter.ResponseMessage(id, msg, data),
                (result) => {
                    if (result.Exception != null) {
                        Debug.LogException(result.Exception);
                    }            
                });        
        }
    }

#endif
}
