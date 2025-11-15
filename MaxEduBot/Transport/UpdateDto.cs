namespace EduMaxBot.Transport;

public class UpdateDto
{
    public string update_type { get; set; } = "";
    public MessageDto? message { get; set; }
}

public class MessageDto
{
    public long timestamp { get; set; }
    public SenderDto sender { get; set; } = new();
    public RecipientDto recipient { get; set; } = new();
    public MessageBodyDto body { get; set; } = new();
}

public class SenderDto
{
    public long user_id { get; set; }
    public string? username { get; set; }
    public string? first_name { get; set; }
    public bool is_bot { get; set; }
}

public class RecipientDto
{
    public long user_id { get; set; }
    public long chat_id { get; set; }
    public string chat_type { get; set; } = "";
}

public class MessageBodyDto
{
    public string? text { get; set; }
}
