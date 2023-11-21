using Microsoft.AspNetCore.Razor.TagHelpers;
using EucRepo.ModelsFilter;

namespace EucRepo.TagHelpers
{
    [HtmlTargetElement("sort-glyph")]
    public class SortGlyphTagHelper : TagHelper
    {
        public string? Column { get; set; }
        public SortByModel? SortByModel { get; set; } 
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (string.Equals(Column, SortByModel?.Column, StringComparison.CurrentCultureIgnoreCase))
            {
                output.TagName = "span";
                output.Attributes.Clear();
                switch (SortByModel?.Order)
                {
                    case "desc":
                        output.Attributes.Add("class", "fa fa-sort-amount-down-alt");
                        break;
                    default:
                        output.Attributes.Add("class", "fa fa-sort-amount-up-alt");
                        break;
                }
            }
            else
            {
                output.SuppressOutput();
            }
        }
    }
}
