using System;

namespace Alejof.Notes.Models
{
    public class Note
    {
        public string Id { get; set; } // Reverse date
        public string Type { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Content { get; set; }
        public string Source { get; set; }
        public string DateText { get; set; }
    }
}
