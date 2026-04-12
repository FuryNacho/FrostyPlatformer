#nullable enable
using System;
using System.Collections.Generic;
using FrostyPlatformer.Models;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Abstraherar highscore-operationer och tillåter states att läsa och skriva
    /// highscore utan att känna till Aggregate.Instance eller fil-I/O.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Dependency Inversion (Interface Segregation)
    ///
    /// MOTIVERING:
    /// States anropade tidigare Aggregate.Instance.PlacesOnHighScore / PutOnHighScore /
    /// GetHighScoreList m.fl. direkt — singleton-beroenden som kopplar states hårt till
    /// Aggregate och omöjliggör testning. IScoreSystem bryter beroendet: states beror
    /// på abstraktionen, ScoreSystem delegerar till Aggregate-implementationen.
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.OnCreate() som new ScoreSystem() och injiceras via
    /// GameServices.Score. States anropar _services.Score.PlacesOnHighScore(ts)
    /// utan Aggregate-referens.
    /// </remarks>
    public interface IScoreSystem
    {
        /// <summary>Returnerar true om <paramref name="time"/> är tillräckligt bra för en topplats.</summary>
        bool PlacesOnHighScore(TimeSpan time);

        /// <summary>Returnerar true om <paramref name="time"/> slår nuvarande förstaplats.</summary>
        bool IsNewFirstPlace(TimeSpan time);

        /// <summary>Lägger till ett nytt highscore-inlägg och sorterar listan. Håller max 5 poster.</summary>
        void PutOnHighScore(HighScoreObj entry);

        /// <summary>Sparar highscore-listan till disk.</summary>
        void Save();

        /// <summary>Returnerar den sorterade highscore-listan.</summary>
        List<HighScoreObj> GetList();

        /// <summary>Nollställer highscore-listan och sparar.</summary>
        void Reset();
    }
}
