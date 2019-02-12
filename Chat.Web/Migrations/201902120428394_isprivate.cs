namespace Chat.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class isprivate : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Messages", "ToRoom_Id", "dbo.Rooms");
            DropIndex("dbo.Messages", new[] { "ToRoom_Id" });
            AddColumn("dbo.Messages", "IsPrivate", c => c.Boolean(nullable: false));
            AlterColumn("dbo.Messages", "ToRoom_Id", c => c.Int());
            CreateIndex("dbo.Messages", "ToRoom_Id");
            AddForeignKey("dbo.Messages", "ToRoom_Id", "dbo.Rooms", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Messages", "ToRoom_Id", "dbo.Rooms");
            DropIndex("dbo.Messages", new[] { "ToRoom_Id" });
            AlterColumn("dbo.Messages", "ToRoom_Id", c => c.Int(nullable: false));
            DropColumn("dbo.Messages", "IsPrivate");
            CreateIndex("dbo.Messages", "ToRoom_Id");
            AddForeignKey("dbo.Messages", "ToRoom_Id", "dbo.Rooms", "Id", cascadeDelete: true);
        }
    }
}
