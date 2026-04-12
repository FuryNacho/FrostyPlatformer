#nullable enable
using System;
using System.Collections.Generic;
using FrostyPlatformer.Core;
using FrostyPlatformer.Models;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Delegerar alla highscore-operationer till Aggregate. Bryter states-beroendet
    /// på Aggregate.Instance utan att duplicera logiken.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter
    ///
    /// MOTIVERING:
    /// Highscore-logiken ägs av Aggregate (laddning, sortering, persistens). Att
    /// flytta den hade inneburit duplicering och ökad risk för divergens. ScoreSystem
    /// är ett tunt lager som exponerar just de operationer states behöver via
    /// IScoreSystem — utan att avslöja Aggregate som helhet.
    ///
    /// ANVÄNDNING:
    /// new ScoreSystem() i Program.OnCreate(). Injiceras via GameServices.Score.
    /// Kräver att Aggregate.Instance är initialiserat innan anrop görs.
    /// </remarks>
    public class ScoreSystem : IScoreSystem
    {
        /// <inheritdoc/>
        public bool PlacesOnHighScore(TimeSpan time) => Aggregate.Instance.PlacesOnHighScore(time);

        /// <inheritdoc/>
        public bool IsNewFirstPlace(TimeSpan time) => Aggregate.Instance.IsNewFirstPlaceHS(time);

        /// <inheritdoc/>
        public void PutOnHighScore(HighScoreObj entry) => Aggregate.Instance.PutOnHighScore(entry);

        /// <inheritdoc/>
        public void Save() => Aggregate.Instance.SaveHighScoreList();

        /// <inheritdoc/>
        public List<HighScoreObj> GetList() => Aggregate.Instance.GetHighScoreList();

        /// <inheritdoc/>
        public void Reset() => Aggregate.Instance.ResetHighScore();
    }
}
