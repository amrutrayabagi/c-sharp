﻿//Build Date: November 24, 2015
using System;
using System.Text;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.Linq;

using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Security;

namespace PubnubApi
{
	internal class PubnubWin : PubnubCore
	{

		#region "Constants"
		LoggingMethod.Level pubnubLogLevel = LoggingMethod.Level.Off;
		PubnubErrorFilter.Level errorLevel = PubnubErrorFilter.Level.Info;

		#if (!SILVERLIGHT && !WINDOWS_PHONE)
		protected bool pubnubEnableProxyConfig = true;
		#endif
		
		#if (__MonoCS__)
		protected string _domainName = "pubsub.pubnub.com";
		#endif

        private object _reconnectFromSuspendMode = null;

		#endregion

		#region "Properties"
		//Proxy
		private PubnubProxy _pubnubProxy = null;
		public PubnubProxy Proxy
		{
			get
			{
		        #if (!SILVERLIGHT && !WINDOWS_PHONE)
                return _pubnubProxy;
                #else
                throw new NotSupportedException("Proxy is not supported");
                #endif
            }
			set
			{
                #if (!SILVERLIGHT && !WINDOWS_PHONE)
                _pubnubProxy = value;
				if (_pubnubProxy == null)
				{
					throw new ArgumentException("Missing Proxy Details");
				}
				if (string.IsNullOrEmpty(_pubnubProxy.ProxyServer) || (_pubnubProxy.ProxyPort <= 0) || string.IsNullOrEmpty(_pubnubProxy.ProxyUserName) || string.IsNullOrEmpty(_pubnubProxy.ProxyPassword))
				{
					_pubnubProxy = null;
					throw new MissingMemberException("Insufficient Proxy Details");
				}
                #else
                throw new NotSupportedException("Proxy is not supported");
                #endif
			}
		}
		#endregion

		#region "Constructors and destructors"

#if (!SILVERLIGHT && !WINDOWS_PHONE && !MONOTOUCH && !__IOS__ && !MONODROID && !__ANDROID__ && !NETFX_CORE)
//        ~PubnubWin()
//		{
//			//detach
//			SystemEvents.PowerModeChanged -= new PowerModeChangedEventHandler (SystemEvents_PowerModeChanged);
//		}
#endif

        public PubnubWin (string publishKey, string subscribeKey): 
			base(publishKey, subscribeKey)
		{
		}

		public PubnubWin(string publishKey, string subscribeKey, string secretKey, string cipherKey, bool sslOn): 
			base(publishKey, subscribeKey, secretKey, cipherKey, sslOn)
		{
		}

		public PubnubWin(string publishKey, string subscribeKey, string secretKey):
			base(publishKey, subscribeKey, secretKey)
		{
		}
		#endregion

		#region "Abstract methods"
		protected override PubnubWebRequest SetServicePointSetTcpKeepAlive (PubnubWebRequest request)
        {
#if ((!__MonoCS__) && (!SILVERLIGHT) && !WINDOWS_PHONE && !NETFX_CORE)
            //request.ServicePoint.SetTcpKeepAlive(true, base.LocalClientHeartbeatInterval * 1000, 1000);
#endif
            //do nothing for mono
			return request;
		}
		protected override PubnubWebRequest SetProxy<T> (PubnubWebRequest request)
        {
#if (!SILVERLIGHT && !WINDOWS_PHONE && !NETFX_CORE)
            if (pubnubEnableProxyConfig && _pubnubProxy != null)
            {
//                LoggingMethod.WriteToLog(string.Format("DateTime {0}, ProxyServer={1}; ProxyPort={2}; ProxyUserName={3}", DateTime.Now.ToString(), _pubnubProxy.ProxyServer, _pubnubProxy.ProxyPort, _pubnubProxy.ProxyUserName), LoggingMethod.LevelInfo);
//                WebProxy webProxy = new WebProxy(_pubnubProxy.ProxyServer, _pubnubProxy.ProxyPort);
//                webProxy.Credentials = new NetworkCredential(_pubnubProxy.ProxyUserName, _pubnubProxy.ProxyPassword);
//                request.Proxy = webProxy;
            }
#endif
            //No proxy setting for WP7
            return request;
		}

		protected override PubnubWebRequest SetTimeout<T>(RequestState<T> pubnubRequestState, PubnubWebRequest request)
        {
#if (!SILVERLIGHT && !WINDOWS_PHONE && !NETFX_CORE)
            //request.Timeout = GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000;
#endif
            //No Timeout setting for WP7
            return request;
		}

		protected override void GeneratePowerSuspendEvent ()
        {
#if (!SILVERLIGHT && !WINDOWS_PHONE && !MONOTOUCH && !__IOS__ && !MONODROID && !__ANDROID__ && !NETFX_CORE)

//            PowerModeChangedEventArgs powerChangeEvent = new PowerModeChangedEventArgs(PowerModes.Suspend);
//            SystemEvents_PowerModeChanged(null, powerChangeEvent);
#endif
            return;
        }

		protected override void GeneratePowerResumeEvent ()
        {
#if (!SILVERLIGHT && !WINDOWS_PHONE && !MONOTOUCH && !__IOS__ && !MONODROID && !__ANDROID__ && !NETFX_CORE)
//            PowerModeChangedEventArgs powerChangeEvent = new PowerModeChangedEventArgs(PowerModes.Resume);
//            SystemEvents_PowerModeChanged(null, powerChangeEvent);
#endif
            return;
        }

		#endregion

		#region "Overridden methods"
		protected override sealed void Init(string publishKey, string subscribeKey, string secretKey, string cipherKey, bool sslOn)
		{
			LoggingMethod.WriteToLog("Using NewtonsoftJsonDotNet", LoggingMethod.LevelInfo);
			base.JsonPluggableLibrary = new NewtonsoftJsonDotNet();

			base.PubnubLogLevel = pubnubLogLevel;
			base.PubnubErrorLevel = errorLevel;

#if (SILVERLIGHT || WINDOWS_PHONE)
            HttpWebRequest.RegisterPrefix("https://", WebRequestCreator.ClientHttp);
            HttpWebRequest.RegisterPrefix("http://", WebRequestCreator.ClientHttp);
#endif

			base.publishKey = publishKey;
			base.subscribeKey = subscribeKey;
			base.secretKey = secretKey;
			base.cipherKey = cipherKey;
			base.ssl = sslOn;

			base.VerifyOrSetSessionUUID();

			//Initiate System Events for PowerModeChanged - to monitor suspend/resume
			InitiatePowerModeCheck();

		}

		protected override bool InternetConnectionStatus(string channel, string channelGroup, Action<PubnubClientError> errorCallback, string[] rawChannels, string[] rawChannelGroups)
		{
            bool networkConnection;
            networkConnection = ClientNetworkStatus.CheckInternetStatus(pubnetSystemActive, errorCallback, rawChannels, rawChannelGroups);
            return networkConnection;
        }

		public override Guid GenerateGuid()
		{
			return base.GenerateGuid();
		}

        protected override void ForceCanonicalPathAndQuery (Uri requestUri)
        {
            LoggingMethod.WriteToLog("Inside ForceCanonicalPathAndQuery = " + requestUri.ToString(), LoggingMethod.LevelInfo);
            try {
                FieldInfo flagsFieldInfo = typeof(Uri).GetField("m_Flags", BindingFlags.Instance | BindingFlags.NonPublic);
                if (flagsFieldInfo != null)
                {
                    ulong flags = (ulong)flagsFieldInfo.GetValue(requestUri);
                    flags &= ~((ulong)0x30); // Flags.PathNotCanonical|Flags.QueryNotCanonical
                    flagsFieldInfo.SetValue(requestUri, flags);
                }
            }
            catch(Exception ex)
            {
                LoggingMethod.WriteToLog("Exception Inside ForceCanonicalPathAndQuery = " + ex.ToString(), LoggingMethod.LevelInfo);
            }
        }

		protected override sealed void SendRequestAndGetResult<T> (Uri requestUri, RequestState<T> pubnubRequestState, PubnubWebRequest request)
        {
#if (SILVERLIGHT || WINDOWS_PHONE || NETFX_CORE)
            //For WP7, Ensure that the RequestURI length <= 1599
            //For SL, Ensure that the RequestURI length <= 1482 for Large Text Message. If RequestURI Length < 1343, Successful Publish occurs
            IAsyncResult asyncResult = request.BeginGetResponse(new AsyncCallback(UrlProcessResponseCallback<T>), pubnubRequestState);
            Timer webRequestTimer = new Timer(OnPubnubWebRequestTimeout<T>, pubnubRequestState, GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000, Timeout.Infinite);
#else
            if (!ClientNetworkStatus.MachineSuspendMode && !PubnubWebRequest.MachineSuspendMode)
            {
                IAsyncResult asyncResult = request.BeginGetResponse(new AsyncCallback(UrlProcessResponseCallback<T>), pubnubRequestState);
				Timer webRequestTimer = new Timer(OnPubnubWebRequestTimeout<T>, pubnubRequestState, GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000, Timeout.Infinite);
                //ThreadPool.RegisterWaitForSingleObject(asyncResult.AsyncWaitHandle, new WaitOrTimerCallback(OnPubnubWebRequestTimeout<T>), pubnubRequestState, GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000, true);
            }
            else
            {
				ReconnectState<T> netState = new ReconnectState<T>();
				netState.Channels = pubnubRequestState.Channels;
				netState.ChannelGroups = pubnubRequestState.ChannelGroups;
				netState.ResponseType = pubnubRequestState.ResponseType;
				netState.SubscribeRegularCallback = pubnubRequestState.SubscribeRegularCallback;
				netState.PresenceRegularCallback = pubnubRequestState.PresenceRegularCallback;
				netState.ErrorCallback = pubnubRequestState.ErrorCallback;
				netState.ConnectCallback = pubnubRequestState.ConnectCallback;
				netState.Timetoken = pubnubRequestState.Timetoken;
				netState.Reconnect = pubnubRequestState.Reconnect;

                _reconnectFromSuspendMode = netState;
                return;
            }
#endif
			if (pubnubRequestState.ResponseType == ResponseType.Presence || pubnubRequestState.ResponseType == ResponseType.Subscribe)
            {
                if (presenceHeartbeatTimer != null)
                {
                    presenceHeartbeatTimer.Dispose();
                    presenceHeartbeatTimer = null;
                }
                if ((pubnubRequestState.Channels != null && pubnubRequestState.Channels.Length > 0 && pubnubRequestState.Channels.Where(s => s.Contains("-pnpres") == false).ToArray().Length > 0)
                    || (pubnubRequestState.ChannelGroups != null && pubnubRequestState.ChannelGroups.Length > 0 && pubnubRequestState.ChannelGroups.Where(s => s.Contains("-pnpres") == false).ToArray().Length > 0))
                {
                    RequestState<T> presenceHeartbeatState = new RequestState<T>();
                    presenceHeartbeatState.Channels = pubnubRequestState.Channels;
                    presenceHeartbeatState.ChannelGroups = pubnubRequestState.ChannelGroups;
					presenceHeartbeatState.ResponseType = ResponseType.PresenceHeartbeat;
                    presenceHeartbeatState.ErrorCallback = pubnubRequestState.ErrorCallback;
                    presenceHeartbeatState.Request = null;
                    presenceHeartbeatState.Response = null;

                    if (base.PresenceHeartbeatInterval > 0)
                    {
                        presenceHeartbeatTimer = new Timer(OnPresenceHeartbeatIntervalTimeout<T>, presenceHeartbeatState, base.PresenceHeartbeatInterval * 1000, base.PresenceHeartbeatInterval * 1000);
                    }
                }
            }
        }

		protected override void TimerWhenOverrideTcpKeepAlive<T> (Uri requestUri, RequestState<T> pubnubRequestState)
		{
			if(localClientHeartBeatTimer != null){
				localClientHeartBeatTimer.Dispose();
			}
			localClientHeartBeatTimer = new Timer(new TimerCallback(OnPubnubLocalClientHeartBeatTimeoutCallback<T>), pubnubRequestState, 0,
                                       base.LocalClientHeartbeatInterval * 1000);
			channelLocalClientHeartbeatTimer.AddOrUpdate(requestUri, localClientHeartBeatTimer, (key, oldState) => localClientHeartBeatTimer);
		}

        protected override void ProcessResponseCallbackExceptionHandler<T>(Exception ex, RequestState<T> asynchRequestState)
        {
            //common Exception handler
#if !NETFX_CORE
//			if (asynchRequestState.Response != null)
//				asynchRequestState.Response.Close ();
#endif

            LoggingMethod.WriteToLog(string.Format("DateTime {0} Exception= {1} for URL: {2}", DateTime.Now.ToString(), ex.ToString(), asynchRequestState.Request.RequestUri.ToString()), LoggingMethod.LevelError);
			UrlRequestCommonExceptionHandler<T>(asynchRequestState.ResponseType, asynchRequestState.Channels, asynchRequestState.ChannelGroups, asynchRequestState.Timeout, asynchRequestState.SubscribeRegularCallback, asynchRequestState.PresenceRegularCallback, asynchRequestState.ConnectCallback, asynchRequestState.WildcardPresenceCallback, asynchRequestState.ErrorCallback, false);
        }

        protected override bool HandleWebException<T>(WebException webEx, RequestState<T> asynchRequestState, string channel, string channelGroup)
        {
            bool reconnect = false;
			if (webEx.Status == WebExceptionStatus.ConnectFailure //Sending Keep-alive packet failed (No network)/Server is down.
				&& !overrideTcpKeepAlive)
            {
                //internet connection problem.
                LoggingMethod.WriteToLog(string.Format("DateTime {0}, _urlRequest - Internet connection problem", DateTime.Now.ToString()), LoggingMethod.LevelError);
				if ((asynchRequestState.ResponseType == ResponseType.Subscribe || asynchRequestState.ResponseType == ResponseType.Presence))
                {
                    if (channelInternetStatus.ContainsKey(channel))
                    {
                        reconnect = true;
                        if (channelInternetStatus[channel])
                        {
                            //Reset Retry if previous state is true
                            channelInternetRetry.AddOrUpdate(channel, 0, (key, oldValue) => 0);
                        }
                        else
                        {
                            channelInternetRetry.AddOrUpdate(channel, 1, (key, oldValue) => oldValue + 1);
                            string multiChannel = (asynchRequestState.Channels != null) ? string.Join(",", asynchRequestState.Channels) : "";
                            string multiChannelGroup = (asynchRequestState.ChannelGroups != null) ? string.Join(",", asynchRequestState.ChannelGroups) : "";
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} {1} channel = {2} _urlRequest - Internet connection retry {3} of {4}", DateTime.Now.ToString(), asynchRequestState.ResponseType, multiChannel, channelInternetRetry[channel], base.NetworkCheckMaxRetries), LoggingMethod.LevelInfo);
                            string message = string.Format("Detected internet connection problem. Retrying connection attempt {0} of {1}", channelInternetRetry[channel], base.NetworkCheckMaxRetries);
                            CallErrorCallback(PubnubErrorSeverity.Warn, PubnubMessageSource.Client, multiChannel, multiChannelGroup, asynchRequestState.ErrorCallback, message, PubnubErrorCode.NoInternetRetryConnect, null, null);
                        }
                        channelInternetStatus[channel] = false;
                    }

                    if (channelGroupInternetStatus.ContainsKey(channelGroup))
                    {
                        reconnect = true;
                        if (channelGroupInternetStatus[channelGroup])
                        {
                            //Reset Retry if previous state is true
                            channelGroupInternetRetry.AddOrUpdate(channelGroup, 0, (key, oldValue) => 0);
                        }
                        else
                        {
                            channelGroupInternetRetry.AddOrUpdate(channelGroup, 1, (key, oldValue) => oldValue + 1);
                            string multiChannel = (asynchRequestState.Channels != null) ? string.Join(",", asynchRequestState.Channels) : "";
                            string multiChannelGroup = (asynchRequestState.ChannelGroups != null) ? string.Join(",", asynchRequestState.ChannelGroups) : "";
                            LoggingMethod.WriteToLog(string.Format("DateTime {0} {1} channelgroup = {2} _urlRequest - Internet connection retry {3} of {4}", DateTime.Now.ToString(), asynchRequestState.ResponseType, multiChannelGroup, channelGroupInternetRetry[channelGroup], base.NetworkCheckMaxRetries), LoggingMethod.LevelInfo);
                            string message = string.Format("Detected internet connection problem. Retrying connection attempt {0} of {1}", channelGroupInternetRetry[channelGroup], base.NetworkCheckMaxRetries);
                            CallErrorCallback(PubnubErrorSeverity.Warn, PubnubMessageSource.Client, multiChannel, multiChannelGroup, asynchRequestState.ErrorCallback, message, PubnubErrorCode.NoInternetRetryConnect, null, null);
                        }
                        channelGroupInternetStatus[channelGroup] = false;
                    }
                }
				//Task.Delay(base.NetworkCheckRetryInterval * 1000);
				new System.Threading.ManualResetEvent(false).WaitOne(base.NetworkCheckRetryInterval * 1000);
            }
            return reconnect;
        }

        protected override void ProcessResponseCallbackWebExceptionHandler<T>(WebException webEx, RequestState<T> asyncRequestState, string channel, string channelGroup)
        {
            bool reconnect = false;
            LoggingMethod.WriteToLog(string.Format("DateTime {0}, WebException: {1}", DateTime.Now.ToString(), webEx.ToString()), LoggingMethod.LevelError);
            if (asyncRequestState != null)
            {
                if (asyncRequestState.Request != null)
                    TerminatePendingWebRequest(asyncRequestState);
            }
            //#elif (!SILVERLIGHT)
            reconnect = HandleWebException(webEx, asyncRequestState, channel, channelGroup);

			UrlRequestCommonExceptionHandler<T>(asyncRequestState.ResponseType, asyncRequestState.Channels, asyncRequestState.ChannelGroups, asyncRequestState.Timeout, asyncRequestState.SubscribeRegularCallback, asyncRequestState.PresenceRegularCallback, asyncRequestState.ConnectCallback, asyncRequestState.WildcardPresenceCallback, asyncRequestState.ErrorCallback, false);
        }

        protected override void UrlProcessResponseCallback<T>(IAsyncResult asynchronousResult)
        {
            List<object> result = new List<object>();

            RequestState<T> asyncRequestState = asynchronousResult.AsyncState as RequestState<T>;

            string channel = "";
            string channelGroup = "";
            if (asyncRequestState != null)
            {
                if (asyncRequestState.Channels != null)
                {
                    channel = (asyncRequestState.Channels.Length > 0) ? string.Join(",", asyncRequestState.Channels) : ",";
                }
                if (asyncRequestState.ChannelGroups != null)
                {
                    channelGroup = string.Join(",", asyncRequestState.ChannelGroups);
                }
            }
            //if (asynchRequestState != null && asynchRequestState.c

            PubnubWebRequest asyncWebRequest = asyncRequestState.Request as PubnubWebRequest;
            try
            {
                if (asyncWebRequest != null)
                {
					PubnubWebResponse asyncWebResponse = (PubnubWebResponse)asyncWebRequest.EndGetResponse(asynchronousResult);
                    {
                        asyncRequestState.Response = asyncWebResponse;

						using (StreamReader streamReader = new StreamReader(asyncWebResponse.GetResponseStream()))
                        {
							if (asyncRequestState.ResponseType == ResponseType.Subscribe || asyncRequestState.ResponseType == ResponseType.Presence)
                            {
                                if (!overrideTcpKeepAlive && (
                                            (channelInternetStatus.ContainsKey(channel) && !channelInternetStatus[channel]) 
                                                || (channelGroupInternetStatus.ContainsKey(channelGroup) && !channelGroupInternetStatus[channelGroup])
                                                ))
                                {
                                    if (asyncRequestState.Channels != null && asyncRequestState.Channels.Length > 0)
                                    {
                                        for (int index = 0; index < asyncRequestState.Channels.Length; index++)
                                        {
                                            string activeChannel = asyncRequestState.Channels[index].ToString();
                                            string activeChannelGroup = "";

                                            string status = "Internet connection available";

											PubnubChannelCallbackKey callbackKey = new PubnubChannelCallbackKey();
											callbackKey.Channel = activeChannel;
											callbackKey.ResponseType = asyncRequestState.ResponseType;

											if (channelCallbacks.Count > 0 && channelCallbacks.ContainsKey(callbackKey))
											{
												object callbackObject;
												bool channelAvailable = channelCallbacks.TryGetValue(callbackKey, out callbackObject);
												if (channelAvailable)
												{
													if (asyncRequestState.ResponseType == ResponseType.Presence)
													{
														PubnubPresenceChannelCallback currentPubnubCallback = callbackObject as PubnubPresenceChannelCallback;

														//TODO: PANDU - Revisit logic on connect callback
														if (currentPubnubCallback != null && channelCallbacks.ContainsKey(callbackKey))
														{
															CallErrorCallback(PubnubErrorSeverity.Info, PubnubMessageSource.Client,
																activeChannel, activeChannelGroup, asyncRequestState.ErrorCallback,
																status, PubnubErrorCode.YesInternet, null, null);
														}
													}
													else
													{
														PubnubSubscribeChannelCallback<T> currentPubnubCallback = callbackObject as PubnubSubscribeChannelCallback<T>;

														//TODO: PANDU - Revisit logic on connect callback
														if (currentPubnubCallback != null && channelCallbacks.ContainsKey(callbackKey))
														{
															CallErrorCallback(PubnubErrorSeverity.Info, PubnubMessageSource.Client,
																activeChannel, activeChannelGroup, asyncRequestState.ErrorCallback,
																status, PubnubErrorCode.YesInternet, null, null);
														}
													}
												}

											}
                                        }
                                    }

                                    if (asyncRequestState.ChannelGroups != null && asyncRequestState.ChannelGroups.Length > 0)
                                    {
                                        for (int index = 0; index < asyncRequestState.ChannelGroups.Length; index++)
                                        {
                                            string activeChannel = "";
                                            string activeChannelGroup = asyncRequestState.ChannelGroups[index].ToString();

                                            string status = "Internet connection available";

											PubnubChannelGroupCallbackKey callbackKey = new PubnubChannelGroupCallbackKey();
											callbackKey.ChannelGroup = activeChannel;
											callbackKey.ResponseType = asyncRequestState.ResponseType;

											if (channelGroupCallbacks.Count > 0 && channelGroupCallbacks.ContainsKey(callbackKey))
											{
												object callbackObject;
												bool channelAvailable = channelGroupCallbacks.TryGetValue(callbackKey, out callbackObject);
												if (channelAvailable)
												{
													if (asyncRequestState.ResponseType == ResponseType.Presence)
													{
														PubnubPresenceChannelGroupCallback currentPubnubCallback = callbackObject as PubnubPresenceChannelGroupCallback;
														if (currentPubnubCallback != null && currentPubnubCallback.ConnectCallback != null)
														{
															CallErrorCallback(PubnubErrorSeverity.Info, PubnubMessageSource.Client,
																activeChannel, activeChannelGroup, asyncRequestState.ErrorCallback,
																status, PubnubErrorCode.YesInternet, null, null);
														}
													}
													else
													{
														PubnubSubscribeChannelGroupCallback<T> currentPubnubCallback = callbackObject as PubnubSubscribeChannelGroupCallback<T>;
														if (currentPubnubCallback != null && currentPubnubCallback.ConnectCallback != null)
														{
															CallErrorCallback(PubnubErrorSeverity.Info, PubnubMessageSource.Client,
																activeChannel, activeChannelGroup, asyncRequestState.ErrorCallback,
																status, PubnubErrorCode.YesInternet, null, null);
														}
													}
												}

											}
                                        }
                                    }
                                }

                                channelInternetStatus.AddOrUpdate(channel, true, (key, oldValue) => true);
                                channelGroupInternetStatus.AddOrUpdate(channelGroup, true, (key, oldValue) => true);
                            }

                            //Deserialize the result
                            string jsonString = streamReader.ReadToEnd();
#if !NETFX_CORE
                            //streamReader.Close ();
#endif

                            LoggingMethod.WriteToLog(string.Format("DateTime {0}, JSON for channel={1} ({2}) ={3}", DateTime.Now.ToString(), channel, asyncRequestState.ResponseType.ToString(), jsonString), LoggingMethod.LevelInfo);

                            if (overrideTcpKeepAlive)
                            {
                                TerminateLocalClientHeartbeatTimer(asyncWebRequest.RequestUri);
                            }

							if (asyncRequestState.ResponseType == ResponseType.PresenceHeartbeat)
                            {
                                if (base.JsonPluggableLibrary.IsDictionaryCompatible(jsonString))
                                {
                                    Dictionary<string, object> deserializeStatus = base.JsonPluggableLibrary.DeserializeToDictionaryOfObject(jsonString);
                                    int statusCode = 0; //default. assuming all is ok 
                                    if (deserializeStatus.ContainsKey("status") && deserializeStatus.ContainsKey("message"))
                                    {
                                        Int32.TryParse(deserializeStatus["status"].ToString(), out statusCode);
                                        string statusMessage = deserializeStatus["message"].ToString();

                                        if (statusCode != 200)
                                        {
                                            PubnubErrorCode pubnubErrorType = PubnubErrorCodeHelper.GetErrorType(statusCode, statusMessage);
                                            int pubnubStatusCode = (int)pubnubErrorType;
                                            string errorDescription = PubnubErrorCodeDescription.GetStatusCodeDescription(pubnubErrorType);

                                            PubnubClientError error = new PubnubClientError(pubnubStatusCode, PubnubErrorSeverity.Critical, statusMessage, PubnubMessageSource.Server, asyncRequestState.Request, asyncRequestState.Response, errorDescription, channel, channelGroup);
                                            GoToCallback(error, asyncRequestState.ErrorCallback);
                                        }
                                    }
                                }
                            }
                            else if (jsonString != "[]")
                            {
                                bool errorCallbackRaised = false;
                                if (base.JsonPluggableLibrary.IsDictionaryCompatible(jsonString))
                                {
                                    Dictionary<string, object> deserializeStatus = base.JsonPluggableLibrary.DeserializeToDictionaryOfObject(jsonString);
                                    int statusCode = 0; //default. assuming all is ok 
                                    if (deserializeStatus.ContainsKey("status") && deserializeStatus.ContainsKey("message"))
                                    {
                                        Int32.TryParse(deserializeStatus["status"].ToString(), out statusCode);
                                        string statusMessage = deserializeStatus["message"].ToString();

                                        if (statusCode != 200)
                                        {
                                            PubnubErrorCode pubnubErrorType = PubnubErrorCodeHelper.GetErrorType(statusCode, statusMessage);
                                            int pubnubStatusCode = (int)pubnubErrorType;
                                            string errorDescription = PubnubErrorCodeDescription.GetStatusCodeDescription(pubnubErrorType);

                                            PubnubClientError error = new PubnubClientError(pubnubStatusCode, PubnubErrorSeverity.Critical, statusMessage, PubnubMessageSource.Server, asyncRequestState.Request, asyncRequestState.Response, errorDescription, channel, channelGroup);
                                            errorCallbackRaised = true;
                                            GoToCallback(error, asyncRequestState.ErrorCallback);
                                        }
                                    }
                                }
                                if (!errorCallbackRaised)
                                {
                                    result = WrapResultBasedOnResponseType<T>(asyncRequestState.ResponseType, jsonString, asyncRequestState.Channels, asyncRequestState.ChannelGroups, asyncRequestState.Reconnect, asyncRequestState.Timetoken, asyncRequestState.Request, asyncRequestState.ErrorCallback);
                                }
                            }
                        }
#if !NETFX_CORE
                        //asyncWebResponse.Close ();
#endif
                    }
                }
                else
                {
                    LoggingMethod.WriteToLog(string.Format("DateTime {0}, Request aborted for channel={1}, channel group={2}", DateTime.Now.ToString(), channel, channelGroup), LoggingMethod.LevelInfo);
                }

                ProcessResponseCallbacks<T>(result, asyncRequestState);

                if ((asyncRequestState.ResponseType == ResponseType.Subscribe || asyncRequestState.ResponseType == ResponseType.Presence) && (result != null) && (result.Count > 0))
                {
                    if (asyncRequestState.Channels != null)
                    {
                        foreach (string currentChannel in asyncRequestState.Channels)
                        {
                            multiChannelSubscribe.AddOrUpdate(currentChannel, Convert.ToInt64(result[1].ToString()), (key, oldValue) => Convert.ToInt64(result[1].ToString()));
                        }
                    }
                    if (asyncRequestState.ChannelGroups != null && asyncRequestState.ChannelGroups.Length > 0)
                    {
                        foreach (string currentChannelGroup in asyncRequestState.ChannelGroups)
                        {
                            multiChannelGroupSubscribe.AddOrUpdate(currentChannelGroup, Convert.ToInt64(result[1].ToString()), (key, oldValue) => Convert.ToInt64(result[1].ToString()));
                        }
                    }
                }

				switch (asyncRequestState.ResponseType)
                {
                    case ResponseType.Subscribe:
                    case ResponseType.Presence:
					MultiplexInternalCallback<T>(asyncRequestState.ResponseType, result, asyncRequestState.SubscribeRegularCallback, asyncRequestState.PresenceRegularCallback, asyncRequestState.ConnectCallback, asyncRequestState.WildcardPresenceCallback, asyncRequestState.ErrorCallback);
					break;
                    default:
                        break;
                }
            }
            catch (WebException webEx)
            {
                HttpStatusCode currentHttpStatusCode;
                if (webEx.Response != null && asyncRequestState != null)
                {
                    if (webEx.Response.GetType().ToString() == "System.Net.HttpWebResponse"
                             || webEx.Response.GetType().ToString() == "MS.Internal.Modern.ClientHttpWebResponse"
                             || webEx.Response.GetType().ToString() == "System.Net.Browser.ClientHttpWebResponse")
                    {
                        currentHttpStatusCode = ((HttpWebResponse)webEx.Response).StatusCode;
                    }
                    else
                    {
                        currentHttpStatusCode = ((PubnubWebResponse)webEx.Response).HttpStatusCode;
                    }
                    PubnubWebResponse exceptionResponse = new PubnubWebResponse(webEx.Response, currentHttpStatusCode);
                    if (exceptionResponse != null)
                    {
                        asyncRequestState.Response = exceptionResponse;

                        using (StreamReader streamReader = new StreamReader(asyncRequestState.Response.GetResponseStream()))
                        {
                            string jsonString = streamReader.ReadToEnd();

#if !NETFX_CORE
                            //streamReader.Close ();
#endif

                            LoggingMethod.WriteToLog(string.Format("DateTime {0}, JSON for channel={1} ({2}) ={3}", DateTime.Now.ToString(), channel, asyncRequestState.ResponseType.ToString(), jsonString), LoggingMethod.LevelInfo);

                            if (overrideTcpKeepAlive)
                            {
                                TerminateLocalClientHeartbeatTimer(asyncWebRequest.RequestUri);
                            }

                            if ((int)currentHttpStatusCode < 200 || (int)currentHttpStatusCode >= 300)
                            {
                                result = null;
                                string errorDescription = "";
                                int pubnubStatusCode = 0;

                                if ((int)currentHttpStatusCode == 500 || (int)currentHttpStatusCode == 502 || (int)currentHttpStatusCode == 503 || (int)currentHttpStatusCode == 504 || (int)currentHttpStatusCode == 414)
                                {
                                    //This status code is not giving json string.
                                    string statusMessage = currentHttpStatusCode.ToString();
                                    PubnubErrorCode pubnubErrorType = PubnubErrorCodeHelper.GetErrorType((int)currentHttpStatusCode, statusMessage);
                                    pubnubStatusCode = (int)pubnubErrorType;
                                    errorDescription = PubnubErrorCodeDescription.GetStatusCodeDescription(pubnubErrorType);
                                }
                                else if (base.JsonPluggableLibrary.IsArrayCompatible(jsonString))
                                {
                                    List<object> deserializeStatus = base.JsonPluggableLibrary.DeserializeToListOfObject(jsonString);
                                    string statusMessage = deserializeStatus[1].ToString();
                                    PubnubErrorCode pubnubErrorType = PubnubErrorCodeHelper.GetErrorType((int)currentHttpStatusCode, statusMessage);
                                    pubnubStatusCode = (int)pubnubErrorType;
                                    errorDescription = PubnubErrorCodeDescription.GetStatusCodeDescription(pubnubErrorType);
                                }
                                else if (base.JsonPluggableLibrary.IsDictionaryCompatible(jsonString))
                                {
                                    Dictionary<string, object> deserializeStatus = base.JsonPluggableLibrary.DeserializeToDictionaryOfObject(jsonString);
                                    string statusMessage = deserializeStatus.ContainsKey("message") ? deserializeStatus["message"].ToString() : (deserializeStatus.ContainsKey("error") ? deserializeStatus["error"].ToString() : jsonString);
                                    PubnubErrorCode pubnubErrorType = PubnubErrorCodeHelper.GetErrorType((int)currentHttpStatusCode, statusMessage);
                                    pubnubStatusCode = (int)pubnubErrorType;
                                    errorDescription = PubnubErrorCodeDescription.GetStatusCodeDescription(pubnubErrorType);
                                }
                                else
                                {
                                    PubnubErrorCode pubnubErrorType = PubnubErrorCodeHelper.GetErrorType((int)currentHttpStatusCode, jsonString);
                                    pubnubStatusCode = (int)pubnubErrorType;
                                    errorDescription = PubnubErrorCodeDescription.GetStatusCodeDescription(pubnubErrorType);
                                }

                                PubnubClientError error = new PubnubClientError(pubnubStatusCode, PubnubErrorSeverity.Critical, jsonString, PubnubMessageSource.Server, asyncRequestState.Request, asyncRequestState.Response, errorDescription, channel, channelGroup);
                                GoToCallback(error, asyncRequestState.ErrorCallback);

                            }
                            else if (jsonString != "[]")
                            {
                                result = WrapResultBasedOnResponseType<T>(asyncRequestState.ResponseType, jsonString, asyncRequestState.Channels, asyncRequestState.ChannelGroups, asyncRequestState.Reconnect, asyncRequestState.Timetoken, asyncRequestState.Request, asyncRequestState.ErrorCallback);
                            }
                            else
                            {
                                result = null;
                            }
                        }
                    }
#if !NETFX_CORE
                    //exceptionResponse.Close ();
#endif

                    if (result != null && result.Count > 0)
                    {
                        ProcessResponseCallbacks<T>(result, asyncRequestState);
                    }

                    if (result == null && currentHttpStatusCode == HttpStatusCode.NotFound
                        && (asyncRequestState.ResponseType == ResponseType.Presence || asyncRequestState.ResponseType == ResponseType.Subscribe)
                        && webEx.Response.GetType().ToString() == "System.Net.Browser.ClientHttpWebResponse")
                    {
                        ProcessResponseCallbackExceptionHandler(webEx, asyncRequestState);
                    }
                }
                else
                {
                    if (asyncRequestState.Channels != null || asyncRequestState.ChannelGroups != null || asyncRequestState.ResponseType == ResponseType.Time)
                    {
                        if (asyncRequestState.ResponseType == ResponseType.Subscribe
                                  || asyncRequestState.ResponseType == ResponseType.Presence)
                        {
                            if ((webEx.Message.IndexOf("The request was aborted: The request was canceled") == -1
                                || webEx.Message.IndexOf("Machine suspend mode enabled. No request will be processed.") == -1)
                                && (webEx.Status != WebExceptionStatus.RequestCanceled))
                            {
                                for (int index = 0; index < asyncRequestState.Channels.Length; index++)
                                {
                                    string activeChannel = (asyncRequestState.Channels != null && asyncRequestState.Channels.Length > 0) 
                                        ? asyncRequestState.Channels[index].ToString() : "";
                                    string activeChannelGroup = (asyncRequestState.ChannelGroups != null && asyncRequestState.ChannelGroups.Length > 0) 
                                        ? asyncRequestState.ChannelGroups[index].ToString() : "";

                                    PubnubChannelCallbackKey callbackKey = new PubnubChannelCallbackKey();
                                    callbackKey.Channel = activeChannel;
                                    callbackKey.ResponseType = asyncRequestState.ResponseType;

                                    if (channelCallbacks.Count > 0 && channelCallbacks.ContainsKey(callbackKey))
                                    {
                                        object callbackObject;
                                        bool channelAvailable = channelCallbacks.TryGetValue(callbackKey, out callbackObject);
										if (channelAvailable)
										{
											if (asyncRequestState.ResponseType == ResponseType.Presence)
											{
												PubnubPresenceChannelCallback currentPubnubCallback = callbackObject as PubnubPresenceChannelCallback;
												if (currentPubnubCallback != null && currentPubnubCallback.ErrorCallback != null)
												{
													PubnubClientError error = CallErrorCallback(PubnubErrorSeverity.Warn, PubnubMessageSource.Client,
														activeChannel, activeChannelGroup, currentPubnubCallback.ErrorCallback,
														webEx, asyncRequestState.Request, asyncRequestState.Response);
													LoggingMethod.WriteToLog(string.Format("DateTime {0}, PubnubClientError = {1}", DateTime.Now.ToString(), error.ToString()), LoggingMethod.LevelInfo);
												}
											}
											else
											{
												PubnubSubscribeChannelCallback<T> currentPubnubCallback = callbackObject as PubnubSubscribeChannelCallback<T>;
												if (currentPubnubCallback != null && currentPubnubCallback.ErrorCallback != null)
												{
													PubnubClientError error = CallErrorCallback(PubnubErrorSeverity.Warn, PubnubMessageSource.Client,
														activeChannel, activeChannelGroup, currentPubnubCallback.ErrorCallback,
														webEx, asyncRequestState.Request, asyncRequestState.Response);
													LoggingMethod.WriteToLog(string.Format("DateTime {0}, PubnubClientError = {1}", DateTime.Now.ToString(), error.ToString()), LoggingMethod.LevelInfo);
												}
											}
										}
                                    }
                                }

                                if (asyncRequestState.ChannelGroups != null)
                                {
                                    for (int index = 0; index < asyncRequestState.ChannelGroups.Length; index++)
                                    {
                                        string activeChannel = (asyncRequestState.Channels != null && asyncRequestState.Channels.Length > 0)
                                            ? asyncRequestState.Channels[index].ToString() : "";
                                        string activeChannelGroup = (asyncRequestState.ChannelGroups != null && asyncRequestState.ChannelGroups.Length > 0)
                                            ? asyncRequestState.ChannelGroups[index].ToString() : "";

                                        PubnubChannelGroupCallbackKey callbackKey = new PubnubChannelGroupCallbackKey();
                                        callbackKey.ChannelGroup = activeChannelGroup;
                                        callbackKey.ResponseType = asyncRequestState.ResponseType;

										if (channelGroupCallbacks.Count > 0 && channelGroupCallbacks.ContainsKey(callbackKey))
										{
											object callbackObject;
											bool channelGroupAvailable = channelGroupCallbacks.TryGetValue(callbackKey, out callbackObject);
											if (channelGroupAvailable)
											{
												if (asyncRequestState.ResponseType == ResponseType.Presence)
												{
													PubnubPresenceChannelGroupCallback currentPubnubCallback = callbackObject as PubnubPresenceChannelGroupCallback;
													if (currentPubnubCallback != null && currentPubnubCallback.ErrorCallback != null)
													{
														PubnubClientError error = CallErrorCallback(PubnubErrorSeverity.Warn, PubnubMessageSource.Client,
															activeChannel, activeChannelGroup, currentPubnubCallback.ErrorCallback,
															webEx, asyncRequestState.Request, asyncRequestState.Response);
														LoggingMethod.WriteToLog(string.Format("DateTime {0}, PubnubClientError = {1}", DateTime.Now.ToString(), error.ToString()), LoggingMethod.LevelInfo);
													}
												}
												else
												{
													PubnubSubscribeChannelGroupCallback<T> currentPubnubCallback = callbackObject as PubnubSubscribeChannelGroupCallback<T>;
													if (currentPubnubCallback != null && currentPubnubCallback.ErrorCallback != null)
													{
														PubnubClientError error = CallErrorCallback(PubnubErrorSeverity.Warn, PubnubMessageSource.Client,
															activeChannel, activeChannelGroup, currentPubnubCallback.ErrorCallback,
															webEx, asyncRequestState.Request, asyncRequestState.Response);
														LoggingMethod.WriteToLog(string.Format("DateTime {0}, PubnubClientError = {1}", DateTime.Now.ToString(), error.ToString()), LoggingMethod.LevelInfo);
													}
												}
											}
										}
                                    }
                                }
                            }
                        }
                        else
                        {
                            PubnubClientError error = CallErrorCallback(PubnubErrorSeverity.Warn, PubnubMessageSource.Client,
                                                                 channel, channelGroup, asyncRequestState.ErrorCallback,
                                                                 webEx, asyncRequestState.Request, asyncRequestState.Response);
                            LoggingMethod.WriteToLog(string.Format("DateTime {0}, PubnubClientError = {1}", DateTime.Now.ToString(), error.ToString()), LoggingMethod.LevelInfo);
                        }
                    }
                    ProcessResponseCallbackWebExceptionHandler<T>(webEx, asyncRequestState, channel, channelGroup);
                }
            }
            catch (Exception ex)
            {
                if (!pubnetSystemActive && ex.Message.IndexOf("The IAsyncResult object was not returned from the corresponding asynchronous method on this class.") == -1)
                {
                    if (asyncRequestState.ResponseType == ResponseType.Subscribe || asyncRequestState.ResponseType == ResponseType.Presence)
                    {
                        if (asyncRequestState.Channels != null && asyncRequestState.Channels.Length > 0)
                        {
                            for (int index = 0; index < asyncRequestState.Channels.Length; index++)
                            {
                                string activeChannel = asyncRequestState.Channels[index].ToString();
                                string activeChannelGroup = (asyncRequestState.ChannelGroups != null && asyncRequestState.ChannelGroups.Length > 0)
                                    ? asyncRequestState.ChannelGroups[index].ToString() : "";

                                PubnubChannelCallbackKey callbackKey = new PubnubChannelCallbackKey();
                                callbackKey.Channel = activeChannel;
                                callbackKey.ResponseType = asyncRequestState.ResponseType;

                                if (channelCallbacks.Count > 0 && channelCallbacks.ContainsKey(callbackKey))
                                {
                                    object callbackObject;
                                    bool channelAvailable = channelCallbacks.TryGetValue(callbackKey, out callbackObject);
									if (channelAvailable)
									{
										if (asyncRequestState.ResponseType == ResponseType.Presence)
										{
											PubnubPresenceChannelCallback currentPubnubCallback = callbackObject as PubnubPresenceChannelCallback;
											if (currentPubnubCallback != null && currentPubnubCallback.ErrorCallback != null)
											{
												CallErrorCallback(PubnubErrorSeverity.Critical, PubnubMessageSource.Client,
													activeChannel, activeChannelGroup, currentPubnubCallback.ErrorCallback, ex, asyncRequestState.Request, asyncRequestState.Response);

											}
										}
										else
										{
											PubnubSubscribeChannelCallback<T> currentPubnubCallback = callbackObject as PubnubSubscribeChannelCallback<T>;
											if (currentPubnubCallback != null && currentPubnubCallback.ErrorCallback != null)
											{
												CallErrorCallback(PubnubErrorSeverity.Critical, PubnubMessageSource.Client,
													activeChannel, activeChannelGroup, currentPubnubCallback.ErrorCallback, ex, asyncRequestState.Request, asyncRequestState.Response);

											}
										}
									}
                                }
                            }
                        }

                        if (asyncRequestState.ChannelGroups != null && asyncRequestState.ChannelGroups.Length > 0)
                        {
                            for (int index = 0; index < asyncRequestState.ChannelGroups.Length; index++)
                            {
                                string activeChannel = (asyncRequestState.Channels != null && asyncRequestState.Channels.Length > 0)
                                    ? asyncRequestState.Channels[index].ToString() : "";
                                string activeChannelGroup = asyncRequestState.ChannelGroups[index].ToString();

                                PubnubChannelGroupCallbackKey callbackKey = new PubnubChannelGroupCallbackKey();
                                callbackKey.ChannelGroup = activeChannelGroup;
                                callbackKey.ResponseType = asyncRequestState.ResponseType;

                                if (channelGroupCallbacks.Count > 0 && channelGroupCallbacks.ContainsKey(callbackKey))
                                {
                                    object callbackObject;
                                    bool channelAvailable = channelGroupCallbacks.TryGetValue(callbackKey, out callbackObject);
									if (channelAvailable)
									{
										if (asyncRequestState.ResponseType == ResponseType.Presence)
										{
											PubnubPresenceChannelGroupCallback currentPubnubCallback = callbackObject as PubnubPresenceChannelGroupCallback;
											if (currentPubnubCallback != null && currentPubnubCallback.ErrorCallback != null)
											{
												CallErrorCallback(PubnubErrorSeverity.Critical, PubnubMessageSource.Client,
													activeChannel, activeChannelGroup, currentPubnubCallback.ErrorCallback, ex, asyncRequestState.Request, asyncRequestState.Response);

											}
										}
										else
										{
											PubnubSubscribeChannelGroupCallback<T> currentPubnubCallback = callbackObject as PubnubSubscribeChannelGroupCallback<T>;
											if (currentPubnubCallback != null && currentPubnubCallback.ErrorCallback != null)
											{
												CallErrorCallback(PubnubErrorSeverity.Critical, PubnubMessageSource.Client,
													activeChannel, activeChannelGroup, currentPubnubCallback.ErrorCallback, ex, asyncRequestState.Request, asyncRequestState.Response);

											}
										}
									}
                                }
                            }
                        }
                        
                    }
                    else
                    {
                        CallErrorCallback(PubnubErrorSeverity.Critical, PubnubMessageSource.Client,
                            channel, channelGroup, asyncRequestState.ErrorCallback, ex, asyncRequestState.Request, asyncRequestState.Response);
                    }

                }
                ProcessResponseCallbackExceptionHandler<T>(ex, asyncRequestState);
            }
        }

		#endregion

		#region "Overridden properties"
		public override IPubnubUnitTest PubnubUnitTest
		{
			get
			{
				return base.PubnubUnitTest;
			}
			set
			{
				base.PubnubUnitTest = value;
			}
		}
		#endregion

		#region "Other methods"

		private void InitiatePowerModeCheck()
        {
//#if (!SILVERLIGHT && !WINDOWS_PHONE && !MONOTOUCH && !__IOS__ && !MONODROID && !__ANDROID__ && !NETFX_CORE)
//			try
//			{
//				SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);
//				LoggingMethod.WriteToLog(string.Format("DateTime {0}, Initiated System Event - PowerModeChanged.", DateTime.Now.ToString()), LoggingMethod.LevelInfo);
//			}
//			catch (Exception ex)
//			{
//				LoggingMethod.WriteToLog(string.Format("DateTime {0} No support for System Event - PowerModeChanged.", DateTime.Now.ToString()), LoggingMethod.LevelError);
//				LoggingMethod.WriteToLog(string.Format("DateTime {0} {1}", DateTime.Now.ToString(), ex.ToString()), LoggingMethod.LevelError);
//			}
//#endif
        }

#if (!SILVERLIGHT && !WINDOWS_PHONE && !MONOTOUCH && !__IOS__ && !MONODROID && !__ANDROID__ && !NETFX_CORE)
//		void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
//		{
//			if (e.Mode == PowerModes.Suspend)
//			{
//				pubnetSystemActive = false;
//				ClientNetworkStatus.MachineSuspendMode = true;
//				PubnubWebRequest.MachineSuspendMode = true;
//				TerminatePendingWebRequest();
//                if (overrideTcpKeepAlive && localClientHeartBeatTimer != null)
//				{
//					localClientHeartBeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
//				}
//
//				LoggingMethod.WriteToLog(string.Format("DateTime {0}, System entered into Suspend Mode.", DateTime.Now.ToString()), LoggingMethod.LevelInfo);
//
//				if (overrideTcpKeepAlive)
//				{
//					LoggingMethod.WriteToLog(string.Format("DateTime {0}, Disabled Timer for heartbeat ", DateTime.Now.ToString()), LoggingMethod.LevelInfo);
//				}
//			}
//			else if (e.Mode == PowerModes.Resume)
//			{
//				pubnetSystemActive = true;
//				ClientNetworkStatus.MachineSuspendMode = false;
//				PubnubWebRequest.MachineSuspendMode = false;
//                if (overrideTcpKeepAlive && localClientHeartBeatTimer != null)
//				{
//					try
//					{
//						localClientHeartBeatTimer.Change(
//                            (-1 == base.LocalClientHeartbeatInterval) ? -1 : base.LocalClientHeartbeatInterval * 1000,
//                            (-1 == base.LocalClientHeartbeatInterval) ? -1 : base.LocalClientHeartbeatInterval * 1000);
//					}
//					catch { }
//				}
//
//				LoggingMethod.WriteToLog(string.Format("DateTime {0}, System entered into Resume/Awake Mode.", DateTime.Now.ToString()), LoggingMethod.LevelInfo);
//
//				if (overrideTcpKeepAlive)
//				{
//					LoggingMethod.WriteToLog(string.Format("DateTime {0}, Enabled Timer for heartbeat ", DateTime.Now.ToString()), LoggingMethod.LevelInfo);
//				}
//
//                ReconnectFromSuspendMode(_reconnectFromSuspendMode);
//                _reconnectFromSuspendMode = null;
//
//			}
//		}
#endif

#if (__MonoCS__)
//		bool RequestIsUnsafe(Uri requestUri)
//		{
//			bool isUnsafe = false;
//			StringBuilder requestMessage = new StringBuilder();
//			if (requestUri.Segments.Length > 7)
//			{
//				for (int i = 7; i < requestUri.Segments.Length; i++)
//				{
//					requestMessage.Append(requestUri.Segments[i]);
//				}
//			}
//			foreach (char ch in requestMessage.ToString().ToCharArray())
//			{
//				if (" ~`!@#$^&*()+=[]\\{}|;':\"./<>?".IndexOf(ch) >= 0)
//				{
//					isUnsafe = true;
//					break;
//				}
//			}
//			return isUnsafe;
//		}
#endif

#if (__MonoCS__) 
		string ParseResponse<T>(string responseString, IAsyncResult asynchronousResult)
		{
			string json = "";
			int pos = responseString.LastIndexOf('\n');
			if ((responseString.StartsWith("HTTP/1.1 ") || responseString.StartsWith("HTTP/1.0 "))
			    && (pos != -1) && responseString.Length >= pos + 1)

			{
				json = responseString.Substring(pos + 1);
			}
			return json;
		}
#endif

#if(MONOTOUCH || __IOS__)      
		/// <summary>
		/// Workaround for the bug described here 
		/// https://bugzilla.xamarin.com/show_bug.cgi?id=6501
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="certificate">Certificate.</param>
		/// <param name="chain">Chain.</param>
		/// <param name="sslPolicyErrors">Ssl policy errors.</param>
		static bool ValidatorUnity (object sender,
		                            System.Security.Cryptography.X509Certificates.X509Certificate
		                            certificate,
		                            System.Security.Cryptography.X509Certificates.X509Chain chain,
		                            System.Net.Security.SslPolicyErrors sslPolicyErrors)
		{
			//TODO:
			return true;
		}
#endif

#if(MONODROID || __ANDROID__)      
		/// <summary>
		/// Workaround for the bug described here 
		/// https://bugzilla.xamarin.com/show_bug.cgi?id=6501
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="certificate">Certificate.</param>
		/// <param name="chain">Chain.</param>
		/// <param name="sslPolicyErrors">Ssl policy errors.</param>
		static bool Validator (object sender,
		                       System.Security.Cryptography.X509Certificates.X509Certificate
		                       certificate,
		                       System.Security.Cryptography.X509Certificates.X509Chain chain,
		                       System.Net.Security.SslPolicyErrors sslPolicyErrors)
		{
			var sslTrustManager = (IX509TrustManager) typeof (AndroidEnvironment)
				.GetField ("sslTrustManager",
				           System.Reflection.BindingFlags.NonPublic |
				           System.Reflection.BindingFlags.Static)
					.GetValue (null);

			Func<Java.Security.Cert.CertificateFactory,
			System.Security.Cryptography.X509Certificates.X509Certificate,
			Java.Security.Cert.X509Certificate> c = (f, v) =>
				f.GenerateCertificate (
					new System.IO.MemoryStream (v.GetRawCertData ()))
					.JavaCast<Java.Security.Cert.X509Certificate>();
			var cFactory = Java.Security.Cert.CertificateFactory.GetInstance (Javax.Net.Ssl.TrustManagerFactory.DefaultAlgorithm);
			var certs = new List<Java.Security.Cert.X509Certificate>(
				chain.ChainElements.Count + 1);
			certs.Add (c (cFactory, certificate));
			foreach (var ce in chain.ChainElements) {
				if (certificate.Equals (ce.Certificate))
					continue;
				certificate = ce.Certificate;
				certs.Add (c (cFactory, certificate));
			}
			try {
				//had to comment this out as sslTrustManager was returning null
				//working on the fix or a workaround
				//sslTrustManager.CheckServerTrusted (certs.ToArray (),
				//                                  Javax.Net.Ssl.TrustManagerFactory.DefaultAlgorithm);
				return true;
			}
			catch (Exception e) {
				throw new Exception("SSL error");
			}
		}
#endif

        #endregion

        #region "Nested Classes"
#if (__MonoCS__)
//		class StateObject<T>
//		{
//			public RequestState<T> RequestState
//			{
//				get;
//				set;
//			}
//
//			public TcpSocketClient tcpClient = null;
//			public Stream netStream = null;
//			public SslStream sslns = null;
//			public const int BufferSize = 2048;
//			public byte[] buffer = new byte[BufferSize];
//			public StringBuilder sb = new StringBuilder();
//			public string requestString = null;
//		}
#endif
        #endregion

    }
}

