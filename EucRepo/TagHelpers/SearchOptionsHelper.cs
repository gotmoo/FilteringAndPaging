using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Threading.Tasks;
using EucRepo.ModelsView;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;

namespace EucRepo.TagHelpers
{
    [HtmlTargetElement("input", Attributes = "search-options")]
    public class InputSearchOptionsTagHelper : TagHelper
    {
        public string[]? SearchOptions { get; set; }
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var tagId = $"{context.UniqueId}-list";
            output.Attributes.SetAttribute("list", tagId);
            var datalist = new TagBuilder("datalist")
            {
                Attributes =
                {
                    ["id"] = tagId
                },
            };
            _ =datalist.RenderStartTag();
            SearchOptions ??= Array.Empty<string>();
            foreach (var item in SearchOptions)
            {
                var listOption = new TagBuilder("option")
                {
                    Attributes =
                    {
                        ["value"] = item
                    },
                    TagRenderMode = TagRenderMode.Normal
                };
                datalist.InnerHtml.AppendHtml(listOption.RenderStartTag());
                datalist.InnerHtml.AppendHtml(listOption.RenderEndTag());
            }
            _ = datalist.RenderEndTag();

            output.PostElement.SetHtmlContent(datalist);
        }
    }
    [HtmlTargetElement("select", Attributes = "search-options")]
    public class SelectSearchOptionsTagHelper : TagHelper
    {
        public string[]? SearchOptions { get; set; }
        public string? Value { get; set; }
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagMode = TagMode.StartTagAndEndTag;
            SearchOptions ??= Array.Empty<string>();
            foreach (var item in SearchOptions)
            {
                var listOption = new TagBuilder("option");
                if (item == Value)
                {
                    listOption.Attributes["selected"] = "selected";
                }

                listOption.InnerHtml.Append(item);
                listOption.TagRenderMode = TagRenderMode.Normal;
                output.PostContent.AppendHtml(listOption.RenderStartTag());
                output.PostContent.AppendHtml(listOption.RenderBody());
                output.PostContent.AppendHtml(listOption.RenderEndTag());
            }
        }
    }
    
}
