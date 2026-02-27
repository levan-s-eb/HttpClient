using Refit;

namespace HttpClient.UsabilityTest.ServiceClients;

public interface IFakeService
{
    [Get("/posts")]
    Task<List<Post>> GetPostsAsync(CancellationToken ct = default);

    [Get("/posts/{id}")]
    Task<Post> GetPostAsync(int id, CancellationToken ct = default);
}

public class Post
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string Title { get; init; } = null!;
    public string Body { get; init; } = null!;
}
