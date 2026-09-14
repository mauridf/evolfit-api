namespace EvolFit.Core.Entities;

/// <summary>
/// Cache local de exercícios da wger API (WGR-002).
/// Não pertence a um usuário — é compartilhado.
/// </summary>
public class WgerExerciseCache
{
    public int Id { get; private set; }
    public int WgerExerciseId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public string? MusclesJson { get; private set; }
    public string? EquipmentJson { get; private set; }
    public string? ImagesJson { get; private set; }
    public DateTime CachedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private WgerExerciseCache() { }

    public static WgerExerciseCache Create(
        int wgerExerciseId,
        string name,
        string? description,
        string? category,
        string? musclesJson,
        string? equipmentJson,
        string? imagesJson,
        TimeSpan ttl)
    {
        var now = DateTime.UtcNow;
        return new WgerExerciseCache
        {
            WgerExerciseId = wgerExerciseId,
            Name = name,
            Description = description,
            Category = category,
            MusclesJson = musclesJson,
            EquipmentJson = equipmentJson,
            ImagesJson = imagesJson,
            CachedAt = now,
            ExpiresAt = now.Add(ttl)
        };
    }

    public bool IsExpired => ExpiresAt <= DateTime.UtcNow;

    public void Refresh(
        string name,
        string? description,
        string? category,
        string? musclesJson,
        string? equipmentJson,
        string? imagesJson,
        TimeSpan ttl)
    {
        Name = name;
        Description = description;
        Category = category;
        MusclesJson = musclesJson;
        EquipmentJson = equipmentJson;
        ImagesJson = imagesJson;
        CachedAt = DateTime.UtcNow;
        ExpiresAt = CachedAt.Add(ttl);
    }
}
