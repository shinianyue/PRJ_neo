using System.Collections.Generic;

namespace test.Story;

public sealed class StorySequence
{
    public string Id { get; set; } = string.Empty;
    public List<StoryLine> Lines { get; set; } = new();
}
