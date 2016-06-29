using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UploadManager.Util;

namespace UploadManager
{
    public class CodestrikerUploadManager
    {
        private readonly string m_Username;
        private readonly string m_Password;
        private string m_ActualTopicUrl;
        private readonly Regex m_Regex = new Regex(".*['?']");

        #region Constants

        // these constants are necessary to build the header for the request - send to codestriker.
        // the original request when submitting a new topic on the codestriker web-interface was sniffed by fiddler
        // and recreated - these constants are the specific header information
        private const string Method = "POST";
        private const string Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
        private const string AcceptLanguage = "Accept-Language: en-US,en;q=0.5";
        private const string AcceptEncoding = "Accept-Encoding: gzip, deflate";
        private const string CookieName = "codestriker_cookie";
        private const string Referer = "/codestriker/codestriker.pl?action=create";
        private const string ContentType = "multipart/form-data; charset=UTF-8; ";
        private const string UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:40.0) Gecko/20100101 Firefox/40.0";
        private const string FormFieldString = "Content-Disposition: form-data; ";

        // the boundary constant is necessary for multipart/form data http requests. the data of the
        // form fields within the request has to be seperated by boundaries.
        // the number is random generated and has no further meaning.
        // within one request it has to be the same
        private const string Boundary = "-----------------------------120251016220462";

        // to indicate the used boundary of the request in the header
        private const string HeaderBoundary = "boundary=---------------------------120251016220462";

        // the boundary at the end of the request (with another two lines after the number)
        private const string MultipartBoundaryEnd = Boundary + "--";

        // error code when no standard browser is set
        private const int NoBrowserErrorCode = -2147467259;

        // field of authorization information in header
        private const string AuthorizationHeaderField = "Authorization";

        #endregion

        public Uri RequestUri { get; set; }


        public string UrlWithPort { get; set; }

        public CodestrikerUploadManager(Uri requestUri, string username, string password)
        {
            RequestUri = requestUri;
            m_Username = username;
            m_Password = password;
        }

        /// <summary>
        /// gets the response of codestriker after submitting a new topic.
        /// this is html text of the confirmation site
        /// </summary>
        /// <param name="response">the response-object of the webrequest</param>
        /// <returns>the html response of codestriker as string</returns>
        private static async Task<string> GetResponseString(WebResponse response)
        {
            string result = null;
            Stream responseStream = response?.GetResponseStream();
            if (responseStream != null)
                using (GZipStream stream = new GZipStream(responseStream, CompressionMode.Decompress))
                using (StreamReader reader = new StreamReader(stream, Encoding.Default))
                {
                    result = await reader.ReadToEndAsync();
                }

            Console.WriteLine(result);
            return result;
        }
        /// <summary>
        /// extracts the link to the created topic 
        /// </summary>
        /// <param name="responseString">the html code of the codestriker response</param>
        /// <returns>the specific link</returns>
        private string ExtractUriFromResponse(string responseString)
        {
            // last item is the link to the topic in Codestriker
            try
            {
                LinkItem topicLinkString = LinkFinder.Find(responseString.ToLower()).Last();
                string originalLink = topicLinkString.Text;
                if (RequestUri.ToString().Contains(":"))
                {
                    originalLink = m_Regex.Replace(originalLink, $"{RequestUri.OriginalString}?");
                }

                string linkToProperties = originalLink.Replace("view", "view_topic_properties");
                var topicUri = linkToProperties;
                return topicUri;
            }
            catch (InvalidOperationException e)
            {
                throw new ArgumentException($"Invalid Argument {nameof(responseString)}", e);
            }
        }

        /// <summary>
        /// shows the prior created topic on the codestriker webinterface
        /// a default browser has to be configured
        /// </summary>
        public void NavigateToTopic()
        {
            try
            {
                if (!string.IsNullOrEmpty(m_ActualTopicUrl))
                {
                    Process.Start(m_ActualTopicUrl);
                }
            }
            catch (Win32Exception e)
            {
                if (e.ErrorCode == NoBrowserErrorCode)
                {
                    Debug.WriteLine(e.Message);
                    throw;
                }

            }
            catch (Exception other)
            {
                Debug.WriteLine(other.Message);
                throw;
            }
        }

        /// <summary>
        /// This method assembles the body of the request
        /// containing all form fields. these fields are necessary for
        /// codestriker to create the a new topic. the structure of the
        /// requeststring corresponds to the original request which is genereted when
        /// creating a new topic with the codestriker webinterface.
        /// 
        /// example of the generated body text:
        /// 
        /// -----------------------------120251016220462
        /// Content-Disposition: form-data; name="action"
        /// submit_new_topic
        /// 
        /// -----------------------------120251016220462
        /// Content-Disposition: form-data; name="obsoletes"
        /// 
        /// -----------------------------120251016220462
        /// Content-Disposition: form-data; name="topic_title"
        /// CodestrikerUploadManager
        /// 
        /// -----------------------------120251016220462
        /// Content-Disposition: form-data; name="topic_description"
        /// Shelveset: "CodestrikerUploadManager;CODESTRIKERTFS\walser" Branch:
        /// 
        /// -----------------------------120251016220462
        /// Content-Disposition: form-data; name="topic_file"; filename="CodestrikerUploadManager"
        /// Content-Type: application/octet-stream
        /// 
        /// diff file content!!!!!!
        /// 
        /// -----------------------------120251016220462
        /// Content-Disposition: form-data; name="start_tag"
        /// 
        /// 
        /// -----------------------------120251016220462
        /// Content-Disposition: form-data; name="end_tag"
        /// 
        /// -----------------------------120251016220462     
        /// Content-Disposition: form-data; name="module"
        /// 
        /// 
        /// -----------------------------120251016220462 
        /// Content-Disposition: form-data; name="repository"
        /// 
        /// 
        /// -----------------------------120251016220462
        /// Content-Disposition: form-data; name="projectid"
        /// 
        /// 
        /// -----------------------------120251016220462
        /// Content-Disposition: form-data; name="bug_ids"
        /// 
        /// 
        /// -----------------------------120251016220462
        /// Content-Disposition: form-data; name="email"
        /// 
        /// 
        /// -----------------------------120251016220462
        /// Content-Disposition: form-data; name="reviewers"
        /// 
        /// 
        /// -----------------------------120251016220462
        /// Content-Disposition: form-data; name="cc" 
        /// 
        /// -----------------------------120251016220462
        /// Content-Disposition: form-data; name="topc_state"
        /// 
        /// 
        /// -----------------------------120251016220462
        /// Content-Disposition: form-data; name=".submit"
        /// 
        /// 
        /// -----------------------------120251016220462--   
        /// 
        /// </summary>
        /// <param name="transportObj">
        /// contains the necessary information in the form fields to fill the request body
        /// </param>
        /// <returns></returns>
        private string GetRequestBody(DiffTransportObject transportObj)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Boundary).Append(Environment.NewLine);                                                                                        
            sb.Append(FormFieldString).Append("name=\"action\"").Append(Environment.NewLine).Append(Environment.NewLine);                           
            sb.Append(transportObj.Action).Append(Environment.NewLine);                                                                             

            sb.Append(Boundary).Append(Environment.NewLine);
            sb.Append(FormFieldString).Append("name=\"obsoletes\"").Append(Environment.NewLine).Append(Environment.NewLine);                        
            sb.Append(transportObj.Obsoletes).Append(Environment.NewLine);                                                                          
                                                                                                                                                    

            sb.Append(Boundary).Append(Environment.NewLine);                                                                                        
            sb.Append(FormFieldString).Append("name=\"topic_title\"").Append(Environment.NewLine).Append(Environment.NewLine);                      
            sb.Append(transportObj.TopicTitle).Append(Environment.NewLine);                                                                         

            sb.Append(Boundary).Append(Environment.NewLine);                                                                                        
            sb.Append(FormFieldString).Append("name=\"topic_description\"").Append(Environment.NewLine).Append(Environment.NewLine);                
            sb.Append(transportObj.TopicDescription).Append(Environment.NewLine);                                                                   

            sb.Append(Boundary).Append(Environment.NewLine);
            sb.Append(FormFieldString).AppendFormat("name=\"topic_file\"; filename=\"{0}\"", transportObj.Filename).Append(Environment.NewLine);    
            sb.Append("Content-Type: application/octet-stream").Append(Environment.NewLine).Append(Environment.NewLine);                            
                                                                                                                                                    

            sb.Append(transportObj.DiffString).Append(Environment.NewLine).Append(Environment.NewLine);                                             


            sb.Append(Boundary).Append(Environment.NewLine);                                                                                        
            sb.Append(FormFieldString).Append("name=\"start_tag\"").Append(Environment.NewLine).Append(Environment.NewLine);                        
            sb.Append(transportObj.StartTag).Append(Environment.NewLine);                                                                           

            sb.Append(Boundary).Append(Environment.NewLine);                                                                                        
            sb.Append(FormFieldString).Append("name=\"end_tag\"").Append(Environment.NewLine).Append(Environment.NewLine);                          
            sb.Append(transportObj.EndTag).Append(Environment.NewLine);                                                                             

            sb.Append(Boundary).Append(Environment.NewLine);                                                                                                                                
            sb.Append(FormFieldString).Append("name=\"module\"").Append(Environment.NewLine).Append(Environment.NewLine);                           
            sb.Append(transportObj.Module).Append(Environment.NewLine);                                                                             

            sb.Append(Boundary).Append(Environment.NewLine);                                                                                        
            sb.Append(FormFieldString).Append("name=\"repository\"").Append(Environment.NewLine).Append(Environment.NewLine);                       
            sb.Append(transportObj.Repository).Append(Environment.NewLine);                                                                         

            sb.Append(Boundary).Append(Environment.NewLine);                                                                                        
            sb.Append(FormFieldString).Append("name=\"projectid\"").Append(Environment.NewLine).Append(Environment.NewLine);                        
            sb.Append(transportObj.ProjectId).Append(Environment.NewLine);                                                                          

            sb.Append(Boundary).Append(Environment.NewLine);                                                                                        
            sb.Append(FormFieldString).Append("name=\"bug_ids\"").Append(Environment.NewLine).Append(Environment.NewLine);                          
            sb.Append(transportObj.BugIds).Append(Environment.NewLine);                                                                             

            sb.Append(Boundary).Append(Environment.NewLine);                                                                                        
            sb.Append(FormFieldString).Append("name=\"email\"").Append(Environment.NewLine).Append(Environment.NewLine);                            
            sb.Append(CleanMutation(transportObj.Email)).Append(Environment.NewLine);                                                               

            sb.Append(Boundary).Append(Environment.NewLine);                                                                                        
            sb.Append(FormFieldString).Append("name=\"reviewers\"").Append(Environment.NewLine).Append(Environment.NewLine);                        
            sb.Append(CleanMutation(transportObj.Reviewers)).Append(Environment.NewLine);                                                           

            sb.Append(Boundary).Append(Environment.NewLine);                                                                                        
            sb.Append(FormFieldString).Append("name=\"cc\"").Append(Environment.NewLine).Append(Environment.NewLine);                               
            sb.Append(CleanMutation(transportObj.EmailCc)).Append(Environment.NewLine);                                                             

            sb.Append(Boundary).Append(Environment.NewLine);                                                                                        
            sb.Append(FormFieldString).Append("name=\"topic_state\"").Append(Environment.NewLine).Append(Environment.NewLine);                      
            sb.Append(transportObj.TopicState).Append(Environment.NewLine);                                                                         

            sb.Append(Boundary).Append(Environment.NewLine);                                                                                        
            sb.Append(FormFieldString).Append("name=\".submit\"").Append(Environment.NewLine).Append(Environment.NewLine);                          
            sb.Append(CleanMutation(transportObj.SubmitName)).Append(Environment.NewLine);                                                          

            sb.Append(MultipartBoundaryEnd);                                                                                                                                                     
                                                                                                                                                                                                                                                  
            return sb.ToString();
        }

        protected string CleanMutation(string str)
        {
            str = str.Replace("ä", "ae");
            str = str.Replace("ö", "oe");
            str = str.Replace("ü", "ue");
            str = str.Replace("Ä", "Ae");
            str = str.Replace("Ö", "Oe");
            str = str.Replace("Ü", "Ue");

            return str;
        }

        /// <summary>
        /// assembles the request information to submit a new topic
        /// first: set all header information
        /// second: write body information on request stream
        /// </summary>
        /// <param name="tranportObj">topic information: title, description ...</param>
        /// <returns>HttpWebRequest object</returns>
        private HttpWebRequest GetRequest(DiffTransportObject tranportObj)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(RequestUri);
            WebHeaderCollection myWebHeaderCollection = request.Headers;
            string requestBody = GetRequestBody(tranportObj);
            byte[] encodedBodyData = Encoding.UTF8.GetBytes(requestBody);

            request.Method = Method;
            request.Host = RequestUri.Host;
            request.Accept = Accept;
            myWebHeaderCollection.Add(AcceptLanguage);
            myWebHeaderCollection.Add(AcceptEncoding);

            CookieContainer cookieCont = new CookieContainer();
            Cookie cookie = new Cookie(CookieName, "") { Domain = RequestUri.Host };
            cookieCont.Add(cookie);

            byte[] credentialBuffer = new UTF8Encoding().GetBytes(m_Username + ":" + m_Password);
            request.Headers[AuthorizationHeaderField] = "Basic " + Convert.ToBase64String(credentialBuffer);

            request.Referer = RequestUri.Host + Referer;
            request.CookieContainer = cookieCont;
            request.ContentType = ContentType + HeaderBoundary;
            //request.ContentLength = encodedBodyData.Length;
            request.ServicePoint.Expect100Continue = false;
            request.UserAgent = UserAgent;

            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(encodedBodyData, 0, encodedBodyData.Length);
            }
            return request;
        }

        /// <summary>
        /// sends the request
        /// waits for the response to extract link to new topic
        /// 
        /// response example:
        /// 
        /// <HTML>
        /// ...
        /// <BODY>
        /// ...
        /// Topic title: NewTest<BR>
        /// Author: a.c @hotmail.com<BR>
        /// Topic URL: <A HREF = "http://192.168.206.130/codestriker/codestriker.pl?action=view&topic=1328228" > http://192.168.206.130/codestriker/codestriker.pl?action=view&topic=1328228</A>
        ///<P>
        /// Email has been sent to: a.c @hotmail.com, Christoph
        /// </BODY>
        /// </HTML>
        /// 
        /// 
        /// the last link in the response text is extracted by this method.
        /// </summary>
        /// <param name="transportObj">topic information: title, description ...</param>
        /// <returns>HttpStatusCode</returns>
        public async Task<HttpStatusCode> SendRequest(DiffTransportObject transportObj)
        {
            HttpStatusCode status = HttpStatusCode.Conflict;
            HttpWebRequest request = GetRequest(transportObj);

            HttpWebResponse response = null;
            m_ActualTopicUrl = null;
            try
            {
                response = (HttpWebResponse)await request.GetResponseAsync();
                string responseString = await GetResponseString(response);

                m_ActualTopicUrl = ExtractUriFromResponse(responseString);
            }
            catch (WebException wex)
            {
                Debug.WriteLine(wex.Status);
                throw;
            }
            catch (ArgumentException ae)
            {
                Debug.WriteLine(ae.Message);
                throw;
            }
            finally
            {
                // ensure that open resource is closed
                if (response != null)
                {
                    response.Close();
                    status = HttpStatusCode.OK;
                }
            }
            return status;
        }
    }
}
