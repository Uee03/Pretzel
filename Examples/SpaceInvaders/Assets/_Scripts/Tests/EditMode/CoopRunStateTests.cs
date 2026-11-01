using NUnit.Framework;
using SpaceInvaders.Coop;

namespace SpaceInvaders.Tests
{
    public sealed class CoopRunStateTests
    {
        [Test]
        public void A_new_player_starts_with_three_lives_and_no_score()
        {
            CoopRunState run = new();

            CoopPlayerRun player = run.AddPlayer(0);

            Assert.AreEqual(CoopRunState.StartingLives, player.Lives);
            Assert.AreEqual(0, player.Score);
            Assert.IsFalse(player.IsEliminated);
        }

        [Test]
        public void Adding_the_same_slot_again_keeps_the_players_score()
        {
            CoopRunState run = new();
            run.AddPlayer(1);
            run.AddScore(1, 30);

            CoopPlayerRun again = run.AddPlayer(1);

            Assert.AreEqual(30, again.Score);
            Assert.AreEqual(1, run.Players.Count);
        }

        [Test]
        public void Each_player_has_their_own_lives()
        {
            CoopRunState run = new();
            run.AddPlayer(0);
            run.AddPlayer(1);

            Assert.AreEqual(2, run.LoseLife(0));
            Assert.AreEqual(1, run.LoseLife(0));

            Assert.AreEqual(1, run.Find(0).Lives);
            Assert.AreEqual(CoopRunState.StartingLives, run.Find(1).Lives);
        }

        [Test]
        public void Losing_lives_stops_at_zero_and_eliminates_the_player()
        {
            CoopRunState run = new();
            run.AddPlayer(0);

            run.LoseLife(0);
            run.LoseLife(0);
            Assert.AreEqual(0, run.LoseLife(0));
            Assert.AreEqual(0, run.LoseLife(0));

            Assert.IsTrue(run.Find(0).IsEliminated);
        }

        [Test]
        public void An_unknown_slot_loses_nothing()
        {
            Assert.AreEqual(-1, new CoopRunState().LoseLife(3));
        }

        [Test]
        public void Scores_are_kept_per_player_and_added_up_for_the_team()
        {
            CoopRunState run = new();
            run.AddPlayer(0);
            run.AddPlayer(1);

            run.AddScore(0, 30);
            run.AddScore(1, 10);
            run.AddScore(0, 20);

            Assert.AreEqual(50, run.Find(0).Score);
            Assert.AreEqual(10, run.Find(1).Score);
            Assert.AreEqual(60, run.TotalScore);
        }

        [Test]
        public void An_eliminated_player_cannot_score_with_a_bullet_still_in_flight()
        {
            CoopRunState run = new();
            run.AddPlayer(0);
            run.EliminateAll();

            run.AddScore(0, 30);

            Assert.AreEqual(0, run.Find(0).Score);
        }

        [Test]
        public void The_game_is_over_only_when_every_player_is_out()
        {
            CoopRunState run = new();
            run.AddPlayer(0);
            run.AddPlayer(1);
            for (int i = 0; i < CoopRunState.StartingLives; i++)
            {
                run.LoseLife(0);
            }

            Assert.IsFalse(run.AllEliminated, "One player is still alive.");

            for (int i = 0; i < CoopRunState.StartingLives; i++)
            {
                run.LoseLife(1);
            }

            Assert.IsTrue(run.AllEliminated);
        }

        [Test]
        public void Only_two_players_can_take_part_and_a_third_slot_is_refused()
        {
            CoopRunState run = new();
            Assert.IsNotNull(run.AddPlayer(0));
            Assert.IsNotNull(run.AddPlayer(1));

            Assert.IsNull(run.AddPlayer(2), "The HUD and the lobby are laid out for two pilots.");
            Assert.IsNull(run.AddPlayer(-1));
            Assert.AreEqual(CoopRunState.MaxPlayers, run.Players.Count);
        }

        [Test]
        public void A_match_with_nobody_in_it_is_not_over()
        {
            Assert.IsFalse(new CoopRunState().AllEliminated);
        }

        [Test]
        public void Eliminate_all_takes_every_life_at_once()
        {
            CoopRunState run = new();
            run.AddPlayer(0);
            run.AddPlayer(1);

            run.EliminateAll();

            Assert.IsTrue(run.AllEliminated);
        }

        [Test]
        public void The_leading_slot_has_the_highest_score_and_a_tie_has_none()
        {
            CoopRunState run = new();
            run.AddPlayer(0);
            run.AddPlayer(1);
            Assert.AreEqual(-1, run.LeadingSlot, "Nobody has scored: a tie.");

            run.AddScore(1, 20);
            Assert.AreEqual(1, run.LeadingSlot);

            run.AddScore(0, 20);
            Assert.AreEqual(-1, run.LeadingSlot, "Equal scores are a tie.");
        }

        [Test]
        public void Removing_a_player_drops_their_slot()
        {
            CoopRunState run = new();
            run.AddPlayer(0);
            run.AddPlayer(1);

            Assert.IsTrue(run.RemovePlayer(1));
            Assert.IsFalse(run.RemovePlayer(1));

            Assert.AreEqual(1, run.Players.Count);
            Assert.IsNull(run.Find(1));
        }

        [Test]
        public void Reset_gives_the_same_players_a_fresh_start()
        {
            CoopRunState run = new();
            run.AddPlayer(0);
            run.AddScore(0, 50);
            run.LoseLife(0);

            run.Reset();

            Assert.AreEqual(CoopRunState.StartingLives, run.Find(0).Lives);
            Assert.AreEqual(0, run.Find(0).Score);
            Assert.AreEqual(1, run.Players.Count);
        }

        [Test]
        public void Changes_raise_the_changed_event()
        {
            CoopRunState run = new();
            int changes = 0;
            run.Changed += () => changes++;

            run.AddPlayer(0);
            run.AddScore(0, 10);
            run.LoseLife(0);

            Assert.AreEqual(3, changes);
        }
    }
}
