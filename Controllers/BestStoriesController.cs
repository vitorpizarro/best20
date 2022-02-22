using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;

namespace BestStories.Controllers
{
    [ApiController]

    public class BestStoriesController : Controller
    {
        private readonly IMemoryCache _memoryCache;

        public BestStoriesController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        [HttpGet("best20")]
        public IEnumerable<BestStories> Get()
        {
            List<BestStories> bs = new List<BestStories>();
            if (_memoryCache.TryGetValue("best20", out List<int> cacheValue))
                _memoryCache.TryGetValue("best20Details", out bs);
            
            string jsonBase = (new WebClient()).DownloadString("https://hacker-news.firebaseio.com/v0/beststories.json");

            List<int> oReturn = JsonConvert.DeserializeObject<List<int>>(jsonBase);

            bool lEquals = true;
            if (cacheValue != null)
                for (int i = 0; i < 20; i++)
                    if (!cacheValue[i].Equals(oReturn[i]))
                    {
                        lEquals = false;
                        break;
                    }

            if (!lEquals || cacheValue == null)
            {
                //bs = new List<BestStories>();


                for (int cont = 0; cont < 20; cont++)
                {
                    string json = (new WebClient()).DownloadString("https://hacker-news.firebaseio.com/v0/item/" + oReturn[cont].ToString() + ".json");

                    XmlObjectSerializer Serializer = new DataContractJsonSerializer(typeof(BestStories));
                    var result = JsonConvert.DeserializeObject<BestStories>(json);
                    
                    bs.Add(new BestStories
                    {
                        title = result.title ?? "",
                        time = result.time,
                        uri = result.uri ?? "",
                        postedBy = result.postedBy ?? " ",
                        score = result.score,
                        commentCount = result.commentCount
                    });
                }

                if (!_memoryCache.TryGetValue("best20", out cacheValue))
                {
                    cacheValue = oReturn;

                    _memoryCache.Set("best20", cacheValue);
                }
                if (!_memoryCache.TryGetValue("best20Details", out List<BestStories> bs2))
                {
                    bs2 = bs;
                    _memoryCache.Set("best20Details", bs2);
                }
            }

            return bs;

        }

        [HttpGet("best202")]
        public IEnumerable<BestStories> Get2()
        {
            if (!_memoryCache.TryGetValue("best20", out List<int> cacheValue))
            {
                cacheValue  = new List<int>();
                List<BestStories> tmpDetail = new List<BestStories>();
                for (int i = 0; i < 20; i++)
                { 
                    cacheValue.Add(i*-1);
                    tmpDetail.Add(null);
                }

                _memoryCache.Set("best20",        cacheValue);
                _memoryCache.Set("best20Details", tmpDetail);
            }

            _memoryCache.TryGetValue("best20Details", out List<BestStories> cacheDetail);

            string jsonBase = (new WebClient()).DownloadString("https://hacker-news.firebaseio.com/v0/beststories.json");

            List<int> oReturn = JsonConvert.DeserializeObject<List<int>>(jsonBase);

            if (cacheValue != null)
            {
                List<int> oldList   = cacheValue.ToList();
                int nPosition = 0;

                foreach (int item in oldList)
                {
                    if (!item.Equals(oReturn[nPosition]))
                    {
                        if (oldList.IndexOf(oReturn[nPosition]) is int nPos && nPos > -1)
                        {
                            cacheValue[nPosition]  = cacheValue[nPos];
                            cacheDetail[nPosition] = cacheDetail[nPos];
                        }
                        else
                        {
                            string json = (new WebClient()).DownloadString("https://hacker-news.firebaseio.com/v0/item/" + oReturn[nPosition].ToString() + ".json");

                            XmlObjectSerializer Serializer = new DataContractJsonSerializer(typeof(BestStories));
                            var result = JsonConvert.DeserializeObject<BestStories>(json);

                            cacheValue[nPosition] = oReturn[nPosition];
                            cacheDetail[nPosition] = new BestStories { title = result.title ?? "",
                                                                       time = result.time,
                                                                       uri = result.uri ?? "",
                                                                       postedBy = result.postedBy ?? " ",
                                                                       score = result.score,
                                                                       commentCount = result.commentCount };
                        }
                    }
                    nPosition++;
                }

                _memoryCache.Set("best20",        cacheValue);
                _memoryCache.Set("best20Details", cacheDetail);
            }

            return cacheDetail;
        }
    }
}
