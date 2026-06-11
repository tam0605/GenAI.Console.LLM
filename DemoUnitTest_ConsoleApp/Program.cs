using System.Text;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

public class Program
{
    static async Task Main()
    {
        // Tìm file Calculator.cs đi ngược lên từ thư mục bin
        string? calculatorPath = FindUpwardFile(AppContext.BaseDirectory, "Calculator.cs");
        if (calculatorPath == null)
        {
            Console.WriteLine("Calculator.cs not found");
            return;
        }

        string methodCode = await File.ReadAllTextAsync(calculatorPath, Encoding.UTF8);

        // Tạo câu lệnh nhắc (prompt) yêu cầu LLM viết test viết bằng xUnit
        var prompt = $"""
        Write a real xUnit test for the following C# method.
        Do not use Moq. Just call the method and assert the result.
        Code:
        {methodCode}
        """;

        // Cấu hình Timeout 6 phút đề phòng Model lớn xử lý lâu
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(6) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "lm-studio");

        // Thiết lập body request gửi tới LM Studio
        var body = new
        {
            model = "google/gemma-4-e4b", // Tên model đã load trên LM Studio
            messages = new[] { new { role = "user", content = prompt } },
            max_tokens = 1000,     // Tăng lên 1000 để model thoải mái suy nghĩ và viết code không bị cắt cụt
            stream = false,
            temperature = 0.2
        };

        var json = System.Text.Json.JsonSerializer.Serialize(body);
        var resp = await client.PostAsync("http://localhost:1234/v1/chat/completions",
                                          new StringContent(json, Encoding.UTF8, "application/json"));

        resp.EnsureSuccessStatusCode();
        var text = await resp.Content.ReadAsStringAsync();

        // ----------------- ĐOẠN XỬ LÝ BÓC TÁCH JSON AN TOÀN CHỐNG NULL -----------------
        var jsonParsed = JObject.Parse(text);

        // 1. Kiểm tra mảng choices trả về từ Server
        var choices = jsonParsed["choices"] as JArray;
        if (choices == null || choices.Count == 0)
        {
            Console.WriteLine("LỖI: LM Studio trả về kết quả rỗng (Mảng 'choices' bị trống hoặc null).");
            Console.WriteLine($"Phản hồi từ Server:\n{text}");
            return;
        }

        // 2. Kiểm tra phần tử message bên trong choice đầu tiên
        var messageToken = choices[0]["message"];
        if (messageToken == null)
        {
            Console.WriteLine("LỖI: Không tìm thấy thuộc tính 'message' trong phản hồi.");
            return;
        }

        // 3. Lấy nội dung code: Ưu tiên trường 'content' truyền thống
        string raw = messageToken["content"]?.ToString() ?? "";

        // 4. Đặc thù Gemma 4: Nếu content trống, tiến hành bóc tách từ phần suy nghĩ 'reasoning_content'
        if (string.IsNullOrWhiteSpace(raw) && messageToken["reasoning_content"] != null)
        {
            raw = messageToken["reasoning_content"]!.ToString();
        }

        // 5. Kiểm tra nếu cả 2 trường đều không có dữ liệu chữ
        if (string.IsNullOrWhiteSpace(raw))
        {
            Console.WriteLine("LỖI: Server có phản hồi nhưng cả 'content' và 'reasoning_content' đều rỗng.");
            return;
        }
        // -----------------------------------------------------------------------------

        string unitTestCode = StripCodeFence(raw);

        // Tìm thư mục dự án Unit_Test và lưu file code test vào đó (khớp chính xác với tên của bạn)
        var unitTestDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(calculatorPath)!, "..", "Unit_Test"));
        Directory.CreateDirectory(unitTestDir);

        string outFile = Path.Combine(unitTestDir, "UnitTest_Generated.cs");
        await File.WriteAllTextAsync(outFile, unitTestCode, Encoding.UTF8);

        Console.WriteLine($"Saved: {outFile}");
    }

    // Hàm bổ trợ tìm kiếm file ngược lên cây thư mục
    static string? FindUpwardFile(string start, string name, int max = 8)
    {
        var d = new DirectoryInfo(start);
        for (int i = 0; i < max && d != null; i++, d = d.Parent)
        {
            string c = Path.Combine(d.FullName, name);
            if (File.Exists(c)) return c;
        }
        return null;
    }

    // Hàm bổ trợ xóa bỏ các ký tự ```csharp do LLM sinh ra
    static string StripCodeFence(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        int a = s.IndexOf("```");
        if (a >= 0)
        {
            int b = s.IndexOf("```", a + 3);
            if (b > a) s = s.Substring(a + 3, b - a - 3);
            s = s.Replace("csharp", "").Replace("cs", "");
        }
        return s.Trim();
    }
}