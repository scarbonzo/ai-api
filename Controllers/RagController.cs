using Microsoft.AspNetCore.Mvc;

namespace ai_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RagController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public RagController()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(10);
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("sup.");
        }

        [HttpPost("url")]
        public async Task<IActionResult> RagQuery([FromBody] QueryRequest request)
        {
            try
            {
                _httpClient.BaseAddress = new Uri(request.Server);
                var inoutApi = await _httpClient.GetAsync(request.Url);
                inoutApi.EnsureSuccessStatusCode();
                var context = await inoutApi.Content.ReadAsStringAsync();

                var prompt = $"Context:\n{context}\n\nQuery: {request.Query}\n\nAnswer:";
                var response = await GenerateResponse(prompt, request.Model, request.Tokens, request.Stream);

                // Return the response
                return Ok(new { Response = response, Context = context });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("text")]
        public async Task<IActionResult> BasicQuery([FromBody] QueryRequest request)
        {
            try
            {
                _httpClient.BaseAddress = new Uri(request.Server);
                
                var prompt = $"Query: {request.Query}\n\nAnswer:";
                var response = await GenerateResponse(prompt, request.Model, request.Tokens, request.Stream);

                // Return the response
                return Ok(new { Response = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        private async Task<string> GenerateResponse(string prompt, string model, int tokens, bool stream)
        {
            var response = await _httpClient.PostAsJsonAsync("generate", new
            {
                model,
                prompt,
                stream,
                options = new { num_ctx = tokens }
            });

            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<GenerateResponse>();

            return data.response;
        }
    }

    public class QueryRequest
    {
        public string Server { get; set; } = "http://localhost:11434/api/";
        public string Query { get; set; }
        public string Url { get; set; } = "";
        public string Model { get; set; }
        public int Tokens { get; set; } = 8196;
        public bool Stream { get; set; } = false;
    }

    public class GenerateResponse
    {
        public string model { get; set; }
        public DateTime created_at { get; set; }
        public string response { get; set; }
        public bool done { get; set; }
    }
}
