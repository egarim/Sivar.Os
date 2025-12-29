namespace Sivar.Os.Shared.Framework.Menu;

/// <summary>
/// Defines the core menu items for common entity types.
/// These are the standard actions available throughout the application.
/// </summary>
public static class CoreMenuItems
{
    /// <summary>
    /// Menu items for Post entities.
    /// </summary>
    public static class Post
    {
        public static readonly IMenuItem Edit = MenuItem
            .Create("post.edit", "post.edit")
            .WithTitle("Edit", "MenuItem_Edit")
            .WithIcon("Edit")
            .WithOrder(10)
            .InGroup("primary")
            .OwnerOnly();

        public static readonly IMenuItem Delete = MenuItem
            .Create("post.delete", "post.delete")
            .WithTitle("Delete", "MenuItem_Delete")
            .WithIcon("Delete")
            .WithOrder(20)
            .InGroup("danger")
            .WithColor("Error")
            .WithConfirmation("Confirm_DeletePost")
            .OwnerOnly();

        public static readonly IMenuItem Share = MenuItem
            .Create("post.share", "post.share")
            .WithTitle("Share", "MenuItem_Share")
            .WithIcon("Share")
            .WithOrder(30)
            .InGroup("share");

        public static readonly IMenuItem CopyLink = MenuItem
            .Create("post.copylink", "post.copylink")
            .WithTitle("Copy Link", "MenuItem_CopyLink")
            .WithIcon("Link")
            .WithOrder(40)
            .InGroup("share");

        public static readonly IMenuItem SavePost = MenuItem
            .Create("post.save", "post.save")
            .WithTitle("Save", "MenuItem_Save")
            .WithIcon("Bookmark")
            .WithOrder(50)
            .InGroup("secondary")
            .AuthenticatedOnly();

        public static readonly IMenuItem Report = MenuItem
            .Create("post.report", "post.report")
            .WithTitle("Report", "MenuItem_Report")
            .WithIcon("Flag")
            .WithOrder(100)
            .InGroup("danger")
            .WithColor("Warning")
            .NonOwnerOnly();

        public static readonly IMenuItem Hide = MenuItem
            .Create("post.hide", "post.hide")
            .WithTitle("Hide", "MenuItem_Hide")
            .WithIcon("VisibilityOff")
            .WithOrder(90)
            .InGroup("secondary")
            .NonOwnerOnly();

        /// <summary>
        /// Gets all post menu items.
        /// </summary>
        public static IEnumerable<IMenuItem> All => new[]
        {
            Edit, Delete, Share, CopyLink, SavePost, Report, Hide
        };
    }

    /// <summary>
    /// Menu items for Comment entities.
    /// </summary>
    public static class Comment
    {
        public static readonly IMenuItem Edit = MenuItem
            .Create("comment.edit", "comment.edit")
            .WithTitle("Edit", "MenuItem_Edit")
            .WithIcon("Edit")
            .WithOrder(10)
            .InGroup("primary")
            .OwnerOnly();

        public static readonly IMenuItem Delete = MenuItem
            .Create("comment.delete", "comment.delete")
            .WithTitle("Delete", "MenuItem_Delete")
            .WithIcon("Delete")
            .WithOrder(20)
            .InGroup("danger")
            .WithColor("Error")
            .WithConfirmation("Confirm_DeleteComment")
            .OwnerOnly();

        public static readonly IMenuItem Reply = MenuItem
            .Create("comment.reply", "comment.reply")
            .WithTitle("Reply", "MenuItem_Reply")
            .WithIcon("Reply")
            .WithOrder(5)
            .InGroup("primary")
            .AuthenticatedOnly();

        public static readonly IMenuItem Report = MenuItem
            .Create("comment.report", "comment.report")
            .WithTitle("Report", "MenuItem_Report")
            .WithIcon("Flag")
            .WithOrder(100)
            .InGroup("danger")
            .WithColor("Warning")
            .NonOwnerOnly();

        /// <summary>
        /// Gets all comment menu items.
        /// </summary>
        public static IEnumerable<IMenuItem> All => new[]
        {
            Edit, Delete, Reply, Report
        };
    }

    /// <summary>
    /// Menu items for Profile entities.
    /// </summary>
    public static class Profile
    {
        public static readonly IMenuItem EditProfile = MenuItem
            .Create("profile.edit", "profile.edit")
            .WithTitle("Edit Profile", "MenuItem_EditProfile")
            .WithIcon("Edit")
            .WithOrder(10)
            .InGroup("primary")
            .OwnerOnly();

        public static readonly IMenuItem Settings = MenuItem
            .Create("profile.settings", "profile.settings")
            .WithTitle("Settings", "MenuItem_Settings")
            .WithIcon("Settings")
            .WithOrder(20)
            .InGroup("primary")
            .OwnerOnly();

        public static readonly IMenuItem ShareProfile = MenuItem
            .Create("profile.share", "profile.share")
            .WithTitle("Share Profile", "MenuItem_ShareProfile")
            .WithIcon("Share")
            .WithOrder(30)
            .InGroup("share");

        public static readonly IMenuItem CopyProfileLink = MenuItem
            .Create("profile.copylink", "profile.copylink")
            .WithTitle("Copy Link", "MenuItem_CopyLink")
            .WithIcon("Link")
            .WithOrder(40)
            .InGroup("share");

        public static readonly IMenuItem Block = MenuItem
            .Create("profile.block", "profile.block")
            .WithTitle("Block", "MenuItem_Block")
            .WithIcon("Block")
            .WithOrder(100)
            .InGroup("danger")
            .WithColor("Error")
            .WithConfirmation("Confirm_BlockProfile")
            .NonOwnerOnly();

        public static readonly IMenuItem Report = MenuItem
            .Create("profile.report", "profile.report")
            .WithTitle("Report", "MenuItem_Report")
            .WithIcon("Flag")
            .WithOrder(110)
            .InGroup("danger")
            .WithColor("Warning")
            .NonOwnerOnly();

        /// <summary>
        /// Gets all profile menu items.
        /// </summary>
        public static IEnumerable<IMenuItem> All => new[]
        {
            EditProfile, Settings, ShareProfile, CopyProfileLink, Block, Report
        };
    }

    /// <summary>
    /// Menu items for Blog entities.
    /// </summary>
    public static class Blog
    {
        public static readonly IMenuItem Edit = MenuItem
            .Create("blog.edit", "blog.edit")
            .WithTitle("Edit", "MenuItem_Edit")
            .WithIcon("Edit")
            .WithOrder(10)
            .InGroup("primary")
            .OwnerOnly();

        public static readonly IMenuItem Delete = MenuItem
            .Create("blog.delete", "blog.delete")
            .WithTitle("Delete", "MenuItem_Delete")
            .WithIcon("Delete")
            .WithOrder(20)
            .InGroup("danger")
            .WithColor("Error")
            .WithConfirmation("Confirm_DeleteBlog")
            .OwnerOnly();

        public static readonly IMenuItem Share = MenuItem
            .Create("blog.share", "blog.share")
            .WithTitle("Share", "MenuItem_Share")
            .WithIcon("Share")
            .WithOrder(30)
            .InGroup("share");

        public static readonly IMenuItem CopyLink = MenuItem
            .Create("blog.copylink", "blog.copylink")
            .WithTitle("Copy Link", "MenuItem_CopyLink")
            .WithIcon("Link")
            .WithOrder(40)
            .InGroup("share");

        /// <summary>
        /// Gets all blog menu items.
        /// </summary>
        public static IEnumerable<IMenuItem> All => new[]
        {
            Edit, Delete, Share, CopyLink
        };
    }
}
