using System.Threading.Tasks;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Services
{
      public abstract class SumberData<THasil>
      {
            protected string BaseUrl { get; }
            protected string ApiKey { get; }

            protected SumberData(string baseUrl, string apiKey)
            {
                  BaseUrl = baseUrl;
                  ApiKey = apiKey;
            }

            public abstract Task<THasil> AmbilData(Wilayah w);
      }
}