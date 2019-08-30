using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Alejof.Notes.Functions.TableStorage;
using Humanizer;

namespace Alejof.Notes.Functions.Mapping
{
    public static class NotesMapper
    {
        public static Models.Note ToListModel(this NoteEntity entity, bool shortenSourceLinks = false) =>
            new Models.Note
            {
                Id = entity.RowKey,
                Type = entity.Type,
                Title = entity.Title,
                Source = shortenSourceLinks ? entity.Source.UrlDomain() : entity.Source,
                DateText = entity.Date.Humanize(utcDate: true),
            };

        public static Models.Note ToModel(this NoteEntity entity) =>
            new Models.Note
            {
                Id = entity.RowKey,
                Type = entity.Type,
                Title = entity.Title,
                Slug = entity.Slug,
                Source = entity.Source,
                HeaderImageUrl = entity.HeaderUri,
                DateText = entity.Date.Humanize(utcDate: true),
            };
            
        public static NoteEntity CopyModel(this NoteEntity entity, Models.Note note)
        {
            entity.Title = note.Title;
            entity.Type = note.Type;
            entity.Slug = note.Slug;
            entity.Source = note.Source;
            entity.HeaderUri = note.HeaderImageUrl;

            return entity;
        }
    }
}
