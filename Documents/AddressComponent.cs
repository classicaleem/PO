using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HRPackage.Documents
{
    public class AddressComponent : IComponent
    {
        private string Title, Name, Address, Gst;

        public AddressComponent(string title, string name, string address, string gst) 
        { 
            Title = title; 
            Name = name; 
            Address = address; 
            Gst = gst; 
        }

        public void Compose(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column => 
            {
                column.Item().Text(Title).SemiBold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(5).Text(Name).Bold();
                column.Item().Text(Address ?? "");
                if(!string.IsNullOrEmpty(Gst)) column.Item().Text($"GSTIN: {Gst}");
            });
        }
    }
}
