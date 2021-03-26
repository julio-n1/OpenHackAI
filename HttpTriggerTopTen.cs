using System.Net;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Team1.Function
{
    public static class HttpTriggerTopTen
    {     
        //define classes for responses
        private class WebApiResponseError
        {
            public string message { get; set; }
        }

        private class WebApiResponseWarning
        {
            public string message { get; set; }
        }

        private class WebApiResponseRecord
        {
            public string recordId { get; set; }
            public Dictionary<string, object> data { get; set; }
            public List<WebApiResponseError> errors { get; set; }
            public List<WebApiResponseWarning> warnings { get; set; }
        }


        // class for results
        class WebApiEnricherResponse
        {
            public List<WebApiResponseRecord> values {get; set;}
        }

        [Function("HttpTriggerTopTen")]
        public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            string recordId = null;
            string originalText = null;

            var log = executionContext.GetLogger("HttpTriggerTopTen");
            
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            log.LogInformation("==>>>>REQUEST BODY [" +  requestBody + "]");

            var responseError = req.CreateResponse(HttpStatusCode.InternalServerError);
            responseError.Headers.Add("Content-Type", "application/json; charset=utf-8");

            // Validation
            if (data?.values == null)
            {
                responseError.WriteString(JsonConvert.SerializeObject((
                    new WebApiResponseRecord () 
                    { 
                        errors = new List<WebApiResponseError>() 
                        { 
                            new WebApiResponseError() 
                            { 
                                message = "Could not find values array" 
                            }  
                        } 
                    } 
                )));
                return responseError;
            }
            if (data?.values.HasValues == false || data?.values.First.HasValues == false)
            {                
               responseError.WriteString(JsonConvert.SerializeObject((
                    new WebApiResponseRecord () 
                    { 
                        errors = new List<WebApiResponseError>() 
                        { 
                            new WebApiResponseError() 
                            { 
                                message = "Could not find valid records in values array" 
                            }  
                        } 
                    } 
                )));
                return responseError;
            }

            WebApiEnricherResponse responseApi = new WebApiEnricherResponse();
            responseApi.values = new List<WebApiResponseRecord>();
            foreach (var record in data?.values)
            {
                recordId = record.recordId?.Value as string;
                originalText = record.data?.text?.Value as string;

                if (recordId == null)
                {
                    responseError.WriteString(JsonConvert.SerializeObject((
                    new WebApiResponseRecord () 
                    { 
                        errors = new List<WebApiResponseError>() 
                        { 
                            new WebApiResponseError() 
                            { 
                                message = "recordId cannot be null" 
                            }  
                        } 
                    })));
                    return responseError;
                }

                // log input
                log.LogInformation("==>>>>REQUEST text [" +  originalText + "]");

                // Put together response.
                WebApiResponseRecord responseRecord = new WebApiResponseRecord();
                responseRecord.data = new Dictionary<string, object>();
                responseRecord.data.Add ( "words", get_top_ten_words(originalText) ); //new Dictionary<string, object>();
                responseRecord.data.Add ( "topten", string.Join(",", get_top_ten_words(originalText) ) );
                responseRecord.recordId = recordId;
                responseRecord.warnings = new List<WebApiResponseWarning>() 
                        { 
                            new WebApiResponseWarning() 
                            { 
                                message = string.Join(",", get_top_ten_words(originalText))
                            }  
                        };
                //responseRecord.data.Add("text", get_top_ten_words(originalText));

                responseApi.values.Add(responseRecord);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            log.LogInformation("==>>>>RESPONSE " +  JsonConvert.SerializeObject(responseApi));
            response.WriteString(JsonConvert.SerializeObject(responseApi));
            //response.WriteString(JsonConvert.SerializeObject(new { values = new { data = new { words = get_top_ten_words(originalText) } } }));
            return response;           
            
        }

        public static List<string> get_top_ten_words(string text) 
        {

            // convert to lowercase
            text = text.ToLowerInvariant();
            List<string> words = text.Split(' ').ToList();

            //remove any non alphabet characters
            var onlyAlphabetRegEx = new Regex(@"^[A-z]+$");
            words = words.Where(f => onlyAlphabetRegEx.IsMatch(f)).ToList();

            // Array of stop words to be ignored
            string[] stopwords = { "", "i", "me", "my", "myself", "we", "our", "ours", "ourselves", "you", 
                "youre", "youve", "youll", "youd", "your", "yours", "yourself", 
                "yourselves", "he", "him", "his", "himself", "she", "shes", "her", 
                "hers", "herself", "it", "its", "itself", "they", "them", "thats",
                "their", "theirs", "themselves", "what", "which", "who", "whom", 
                "this", "that", "thatll", "these", "those", "am", "is", "are", "was",
                "were", "be", "been", "being", "have", "has", "had", "having", "do", 
                "does", "did", "doing", "a", "an", "the", "and", "but", "if", "or", 
                "because", "as", "until", "while", "of", "at", "by", "for", "with", 
                "about", "against", "between", "into", "through", "during", "before", 
                "after", "above", "below", "to", "from", "up", "down", "in", "out", 
                "on", "off", "over", "under", "again", "further", "then", "once", "here", 
                "there", "when", "where", "why", "how", "all", "any", "both", "each", 
                "few", "more", "most", "other", "some", "such", "no", "nor", "not", 
                "only", "own", "same", "so", "than", "too", "very", "s", "t", "can", 
                "will", "just", "don", "dont", "should", "shouldve", "now", "d", "ll",
                "m", "o", "re", "ve", "y", "ain", "aren", "arent", "couldn", "couldnt", 
                "didn", "didnt", "doesn", "doesnt", "hadn", "hadnt", "hasn", "hasnt", 
                "havent", "isn", "isnt", "ma", "mightn", "mightnt", "mustn", 
                "mustnt", "needn", "neednt", "shan", "shant", "shall", "shouldn", "shouldnt", "wasn", 
                "wasnt", "weren", "werent", "won", "wont", "wouldn", "wouldnt"}; 
            words = words.Where(x => !stopwords.Contains(x)).ToList();

            // Find distict keywords by key and count, and then order by count.
            var keywords = words.GroupBy(x => x).OrderByDescending(x => x.Count());
            var klist = keywords.ToList();
            var numofWords = 10;
            if(klist.Count<10)
                numofWords=klist.Count;
            
            // Return the first 10 words
            List<string> resList = new List<string>();
            for (int i = 0; i < numofWords; i++)
            {
                resList.Add(klist[i].Key);
            }

            // return the results object
            return resList;
        }
    }
}
