﻿using Quicken.Core.Index.Entities.Models;
using Quicken.Core.Index.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Quicken.Core.Index.Repositories
{
    internal class TargetRepository : RepositoryBase
    {
        #region Constructors

        public TargetRepository()
        {
            // Perform a basic query to load the EF meta-data, and thus make sure that the first query is not super slow
            // http://stackoverflow.com/questions/3891125/entity-framework-first-query-slow
            this.DataContext.Database.ExecuteSqlCommand("SELECT GETDATE()", new object[0]);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updates the targets.
        /// </summary>
        /// <param name="executables">The executables.</param>
        internal void UpdateTargets(IList<Target> input)
        {
            var existing = this.GetTargets();

            var merge =
                from i in input
                join e in existing on i.Path equals e.Path into m
                from e in m.DefaultIfEmpty()
                select new
                {
                    current = e,
                    changed = i
                };

            foreach (var item in merge)
            {
                if (item.current == null)
                {
                    // Insert this new target
                    item.changed.UpdatedDate = DateTime.Now;
                    this.DataContext.Targets.Add(item.changed);
                }
                else
                {
                    // Update all properties of the existing target.
                    // Note that we do not need to update the path here 
                    // though, since that is what we use to find these.
                    item.current.UpdatedDate = DateTime.Now;
                    item.current.Name = item.changed.Name;
                    item.current.Description = item.changed.Description;
                    item.current.Icon = item.changed.Icon;

                    // Update the aliases; remove old ones, and add 
                    // new ones. Leave all others alone.
                    var obsoleteAliases = item.current.Aliases
                        .Where(
                            currentAlias => !item.changed.Aliases
                                .Any(
                                    changedAlias => changedAlias.Text == currentAlias.Text))
                        .ToList();

                    foreach (var obsoleteAlias in obsoleteAliases)
                    {
                        this.DataContext.Aliases.Remove(obsoleteAlias);
                    }

                    var newAliases = item.changed.Aliases
                        .Where(
                            changedAlias => !item.current.Aliases
                                .Any(
                                    currentAlias => currentAlias.Text == changedAlias.Text));

                    foreach (var newAlias in newAliases)
                    {
                        item.current.Aliases.Add(
                            new Alias()
                            {
                                Text = newAlias.Text
                            });
                    }
                }
            }

            foreach (var target in existing.Except(merge.Select(m => m.current)).ToList())
            {
                this.DataContext.Targets.Remove(target);
            }

            this.DataContext.SaveChanges();
        }

        /// <summary>
        /// Gets the targets.
        /// </summary>
        /// <returns></returns>
        internal IList<Target> GetTargets()
        {
            return this.DataContext.Targets.ToList();
        }

        /// <summary>
        /// Finds the targets.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        internal IList<Target> FindTargets(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var results = this.DataContext
                    .Aliases
                    .Where(
                        alias =>
                            alias.Text.StartsWith(text.ToUpper()) ||
                            alias.Text.IndexOf(" " + text.ToUpper()) >= 0)
                    .Select(alias => alias.Target)
                    .OrderByDescending(
                        target =>
                            target.Terms
                                .Max(
                                    term =>
                                        text.ToUpper().StartsWith(term.Text.ToUpper()) ? term.Text.Length : 0))
                    .ThenBy(target => target.Name.ToUpper().IndexOf(text.ToUpper()))
                    .ThenBy(target => target.Name)
                    .ToList();

                return results;
            }

            return new List<Target>();
        }

        /// <summary>
        /// Determines whether this instance has targets.
        /// </summary>
        /// <returns></returns>
        internal bool HasTargets()
        {
            return this.DataContext.Targets.Any();
        }

        /// <summary>
        /// Updates the term target.
        /// </summary>
        /// <param name="targetId">The target identifier.</param>
        /// <param name="text">The text.</param>
        internal void UpdateTermTarget(int targetId, string text)
        {
            // Find the closest existing term to the text entered.
            var closestTerm = this.DataContext
                .Terms
                .Where(t => text.ToUpper().StartsWith(t.Text))
                .OrderByDescending(t => t.Text.Length)
                .FirstOrDefault();

            if (closestTerm == null || closestTerm.TargetId != targetId && closestTerm.Text != text.ToUpper())
            {
                // There is no closest term, or the closest term is associated 
                // with another target, so add a new term to the database.
                var term = new Term()
                {
                    Text = text.ToUpper(),
                    TargetId = targetId
                };

                this.DataContext.Terms.Add(term);
            }
            else if (closestTerm.Text == text.ToUpper() && closestTerm.TargetId != targetId)
            {
                // There is an exact-match term, but it focuses on another target, so re-target it.
                closestTerm.TargetId = targetId;

                // Since we've retargetted this term, see if there are any other more specific
                // terms for it already (check-back). If there are, then this term is no 
                // longer needed and can be removed.
                var moreSpecificTerm = this.DataContext
                    .Terms
                    .Where(t => text.ToUpper().StartsWith(t.Text) && text.ToUpper() != t.Text)
                    .OrderByDescending(t => t.Text.Length)
                    .FirstOrDefault();

                if (moreSpecificTerm != null)
                {
                    this.DataContext.Terms.Remove(closestTerm);
                }
            }

            // Find out if there are any more specific terms that point
            // the to same target, and remove them (check-forward).
            this.DataContext.Terms.RemoveRange(
                this.DataContext
                    .Terms
                    .Where(
                        t =>
                            t.Text.StartsWith(text.ToUpper()) &&
                            t.Text != text.ToUpper() &&
                            t.TargetId == targetId));

            this.DataContext.SaveChanges();
        } 

        #endregion
    }
}
