namespace Triathlon.Web.Domain.Content;

/// <summary>
/// The shapes a CMS page is built from. Each value names a partial under
/// <c>Areas/Public/Shared/_Blocks/</c>, so adding a block type is adding a view beside this list.
/// </summary>
public enum BlockType
{
    Hero = 1,
    RichText,
    Steps,
    Cards,
    Table,
    Faq,
    Cta,
    Clubs,
}
