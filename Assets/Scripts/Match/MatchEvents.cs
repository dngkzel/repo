using System;
using System.Collections.Generic;
using UnityEngine;
using FootballGame.Player;

namespace FootballGame.Match
{
    // =========================================================================
    // Card type
    // =========================================================================

    public enum CardType
    {
        Yellow,
        Red
    }

    // =========================================================================
    // Abstract base
    // =========================================================================

    [Serializable]
    public abstract class MatchEvent
    {
        public int    minute;
        public string description;

        protected MatchEvent(int minute)
        {
            this.minute = minute;
        }
    }

    // =========================================================================
    // Concrete event types
    // =========================================================================

    [Serializable]
    public class GoalEvent : MatchEvent
    {
        public PlayerData scorer;
        public PlayerData assister;
        public bool       isOwnGoal;
        public string     team;

        public GoalEvent(int minute, PlayerData scorer, PlayerData assister,
                         bool isOwnGoal, string team) : base(minute)
        {
            this.scorer    = scorer;
            this.assister  = assister;
            this.isOwnGoal = isOwnGoal;
            this.team      = team;
        }
    }

    [Serializable]
    public class CardEvent : MatchEvent
    {
        public PlayerData player;
        public CardType   type;
        public string     reason;
        public string     team;

        public CardEvent(int minute, PlayerData player, CardType type,
                         string reason, string team) : base(minute)
        {
            this.player = player;
            this.type   = type;
            this.reason = reason;
            this.team   = team;
        }
    }

    [Serializable]
    public class SubstitutionEvent : MatchEvent
    {
        public PlayerData playerIn;
        public PlayerData playerOut;
        public string     team;

        public SubstitutionEvent(int minute, PlayerData playerIn,
                                 PlayerData playerOut, string team) : base(minute)
        {
            this.playerIn  = playerIn;
            this.playerOut = playerOut;
            this.team      = team;
        }
    }

    [Serializable]
    public class FoulEvent : MatchEvent
    {
        public PlayerData fouler;
        public PlayerData fouled;
        public bool       isBookable;
        public string     team;

        public FoulEvent(int minute, PlayerData fouler, PlayerData fouled,
                         bool isBookable, string team) : base(minute)
        {
            this.fouler     = fouler;
            this.fouled     = fouled;
            this.isBookable = isBookable;
            this.team       = team;
        }
    }

    [Serializable]
    public class CornerEvent : MatchEvent
    {
        public string team;

        public CornerEvent(int minute, string team) : base(minute)
        {
            this.team = team;
        }
    }

    [Serializable]
    public class PenaltyEvent : MatchEvent
    {
        public PlayerData taker;
        public bool       scored;
        public string     team;

        public PenaltyEvent(int minute, PlayerData taker, bool scored,
                            string team) : base(minute)
        {
            this.taker  = taker;
            this.scored = scored;
            this.team   = team;
        }
    }

    [Serializable]
    public class OffsidesEvent : MatchEvent
    {
        public string team;

        public OffsidesEvent(int minute, string team) : base(minute)
        {
            this.team = team;
        }
    }

    [Serializable]
    public class ShotEvent : MatchEvent
    {
        public PlayerData shooter;
        public bool       onTarget;
        public bool       isGoal;
        public string     team;

        public ShotEvent(int minute, PlayerData shooter, bool onTarget,
                         bool isGoal, string team) : base(minute)
        {
            this.shooter  = shooter;
            this.onTarget = onTarget;
            this.isGoal   = isGoal;
            this.team     = team;
        }
    }

    // =========================================================================
    // Commentary generator
    // =========================================================================

    public static class CommentaryGenerator
    {
        // ── Goal ─────────────────────────────────────────────────────────────

        private static readonly string[] GoalTemplates =
        {
            "GOAL! {0} finds the net for {2}! What a strike!",
            "It's in! {0} makes no mistake to put {2} ahead!",
            "{0} scores! The crowd goes absolutely wild! {2} take the lead!",
            "GOOOAL! {0} with a stunning finish! {2} are on the board!",
            "What a moment! {0} slots it home for {2}!",
            "{0} does it again! Another goal for {2} and the stadium erupts!",
            "Clinical finish from {0}! {2} are celebrating now!",
        };

        private static readonly string[] OwnGoalTemplates =
        {
            "Oh no! {0} puts it into his own net! Unfortunate moment for {2}!",
            "Disaster! An own goal from {0} gifts the opposition a goal!",
            "What bad luck! {0} turns it into his own net! {2} will be devastated!",
            "Calamity! {0} beats his own goalkeeper! An own goal for {2}!",
            "Unlucky! {0} can't get out of the way and it's an own goal for {2}!",
        };

        private static readonly string[] AssistedGoalTemplates =
        {
            "GOAL! {1} plays it through to {0} who finishes brilliantly for {2}!",
            "{0} scores for {2}! Wonderful assist from {1}!",
            "What a combination! {1} to {0} and it's a goal for {2}!",
        };

        public static string GenerateGoalCommentary(GoalEvent evt)
        {
            if (evt == null) return string.Empty;

            string scorerName = evt.scorer?.name ?? "Unknown";
            string team       = evt.team ?? "the team";

            if (evt.isOwnGoal)
            {
                string[] t = OwnGoalTemplates;
                return string.Format(t[UnityEngine.Random.Range(0, t.Length)], scorerName, "", team);
            }

            if (evt.assister != null && UnityEngine.Random.value > 0.4f)
            {
                string[] t = AssistedGoalTemplates;
                return string.Format(t[UnityEngine.Random.Range(0, t.Length)],
                    scorerName, evt.assister.name, team);
            }

            string[] templates = GoalTemplates;
            return string.Format(templates[UnityEngine.Random.Range(0, templates.Length)],
                scorerName, "", team);
        }

        // ── Card ─────────────────────────────────────────────────────────────

        private static readonly string[] YellowCardTemplates =
        {
            "{0} picks up a yellow card! {2} will have to be careful.",
            "Caution shown to {0}! That's a booking for {2}.",
            "The referee produces the yellow card for {0} of {2}.",
            "{0} is in the book! {2} down to the wire on discipline.",
            "Yellow for {0}! {2} must not afford another one.",
            "Reckless challenge by {0} earns a yellow card for {2}.",
        };

        private static readonly string[] RedCardTemplates =
        {
            "RED CARD! {0} is sent off! {2} are down to ten men!",
            "Off you go! {0} receives a straight red! {2} are in trouble!",
            "Shocking challenge from {0}! Red card and {2} lose a player!",
            "The referee has no hesitation! {0} is dismissed! {2} are reduced to ten!",
            "Second yellow for {0} — it's a red! {2} face an uphill battle!",
            "{0} is given his marching orders! {2} must now fight with ten men!",
        };

        public static string GenerateCardCommentary(CardEvent evt)
        {
            if (evt == null) return string.Empty;

            string playerName = evt.player?.name ?? "Unknown";
            string team       = evt.team ?? "the team";

            if (evt.type == CardType.Red)
            {
                string[] t = RedCardTemplates;
                return string.Format(t[UnityEngine.Random.Range(0, t.Length)], playerName, "", team);
            }
            else
            {
                string[] t = YellowCardTemplates;
                return string.Format(t[UnityEngine.Random.Range(0, t.Length)], playerName, "", team);
            }
        }

        // ── Substitution ──────────────────────────────────────────────────────

        private static readonly string[] SubstitutionTemplates =
        {
            "Substitution for {2}! {1} comes off and {0} enters the pitch.",
            "{2} make a change — {0} replaces {1}.",
            "Tactical switch for {2}: {0} on, {1} off.",
            "{1} makes way for {0} as {2} look to change things up.",
            "Fresh legs for {2}! {0} is on in place of {1}.",
            "{2} bring on {0} for {1}. Interesting decision from the bench.",
            "The manager has seen enough from {1}. {0} comes on for {2}.",
        };

        public static string GenerateSubstitutionCommentary(SubstitutionEvent evt)
        {
            if (evt == null) return string.Empty;

            string playerInName  = evt.playerIn?.name  ?? "Unknown";
            string playerOutName = evt.playerOut?.name ?? "Unknown";
            string team          = evt.team ?? "the team";

            string[] templates = SubstitutionTemplates;
            return string.Format(templates[UnityEngine.Random.Range(0, templates.Length)],
                playerInName, playerOutName, team);
        }

        // ── Foul ─────────────────────────────────────────────────────────────

        private static readonly string[] FoulTemplates =
        {
            "{0} brings down {1} — foul given.",
            "Free kick awarded after {0} clips {1}.",
            "Rough challenge from {0} on {1}. The referee stops play.",
            "{0} takes out {1} — the referee points to where the foul occurred.",
            "Late tackle from {0} on {1}. Free kick.",
            "Cynical foul from {0} to halt {1}'s run.",
        };

        public static string GenerateFoulCommentary(FoulEvent evt)
        {
            if (evt == null) return string.Empty;

            string foulerName = evt.fouler?.name ?? "Unknown";
            string fouledName = evt.fouled?.name ?? "Unknown";

            string[] templates = FoulTemplates;
            return string.Format(templates[UnityEngine.Random.Range(0, templates.Length)],
                foulerName, fouledName);
        }

        // ── Shot ─────────────────────────────────────────────────────────────

        private static readonly string[] ShotOnTargetTemplates =
        {
            "{0} tests the goalkeeper with a powerful effort!",
            "Good save! {0} forces the keeper into action.",
            "{0} shoots! The goalkeeper pushes it wide.",
            "Shot from {0}! Right at the goalkeeper.",
            "{0} curls one towards goal — the keeper claims it.",
            "What a strike from {0}! The keeper just about gets there.",
        };

        private static readonly string[] ShotOffTargetTemplates =
        {
            "{0} blazes it over the bar! That was wayward.",
            "Shot from {0} — well wide of the target.",
            "{0} has a go but it sails into the stands.",
            "Effort from {0} — that drifted well off target.",
            "{0} tries from distance but couldn't find the frame.",
            "Disappointing from {0} — the shot was never troubling the goalkeeper.",
        };

        public static string GenerateShotCommentary(ShotEvent evt)
        {
            if (evt == null) return string.Empty;

            string shooterName = evt.shooter?.name ?? "Unknown";

            if (evt.onTarget)
            {
                string[] t = ShotOnTargetTemplates;
                return string.Format(t[UnityEngine.Random.Range(0, t.Length)], shooterName);
            }
            else
            {
                string[] t = ShotOffTargetTemplates;
                return string.Format(t[UnityEngine.Random.Range(0, t.Length)], shooterName);
            }
        }

        // ── Corner ────────────────────────────────────────────────────────────

        private static readonly string[] CornerTemplates =
        {
            "Corner kick for {0}.",
            "{0} win a corner. Set piece opportunity coming up.",
            "The ball goes out — corner to {0}.",
            "Corner awarded to {0}. Danger in the air!",
            "{0} will take the corner from the right side.",
            "Another corner for {0} as they keep the pressure on.",
        };

        public static string GenerateCornerCommentary(CornerEvent evt)
        {
            if (evt == null) return string.Empty;

            string[] templates = CornerTemplates;
            return string.Format(templates[UnityEngine.Random.Range(0, templates.Length)], evt.team);
        }

        // ── Penalty ───────────────────────────────────────────────────────────

        private static readonly string[] PenaltyScoreTemplates =
        {
            "PENALTY SCORED! {0} sends the goalkeeper the wrong way!",
            "{0} is ice cool from the spot! That's a goal!",
            "No mistake from {0}! Penalty converted for {1}!",
            "{0} steps up and dispatches the penalty with confidence!",
            "Goal from the spot! {0} keeps his nerve for {1}!",
            "{0} slots the penalty home! The pressure is immense!",
        };

        private static readonly string[] PenaltyMissedTemplates =
        {
            "PENALTY MISSED! {0} puts it wide — incredible miss!",
            "The goalkeeper saves it! {0} can't believe it!",
            "Off the post! {0} will be devastated! {1} breathe a sigh of relief!",
            "{0} blazes the penalty over the bar! Shocking miss!",
            "Saved! The goalkeeper guesses right and {0} misses for {1}!",
            "What a moment! {0} misses the penalty and {1} survive!",
        };

        public static string GeneratePenaltyCommentary(PenaltyEvent evt)
        {
            if (evt == null) return string.Empty;

            string takerName = evt.taker?.name ?? "Unknown";
            string team      = evt.team ?? "the team";

            if (evt.scored)
            {
                string[] t = PenaltyScoreTemplates;
                return string.Format(t[UnityEngine.Random.Range(0, t.Length)], takerName, team);
            }
            else
            {
                string[] t = PenaltyMissedTemplates;
                return string.Format(t[UnityEngine.Random.Range(0, t.Length)], takerName, team);
            }
        }

        // ── Offsides ──────────────────────────────────────────────────────────

        private static readonly string[] OffsideTemplates =
        {
            "Offside! {0}'s attack is cut short by the flag.",
            "The assistant referee raises the flag — offside against {0}.",
            "No goal — offside! {0} are frustrated.",
            "Tight call but {0} are flagged offside. Free kick to the defence.",
            "Offside trap works perfectly against {0}.",
            "The linesman's flag denies {0}. Marginal call.",
        };

        public static string GenerateOffsideCommentary(OffsidesEvent evt)
        {
            if (evt == null) return string.Empty;

            string[] templates = OffsideTemplates;
            return string.Format(templates[UnityEngine.Random.Range(0, templates.Length)], evt.team);
        }
    }
}
