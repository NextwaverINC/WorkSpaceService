using System;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml;
using System.Net;
using System.IO;
using System.Configuration;
using System.Data;
using System.Text;
using System.Collections.Generic;
[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class Service : System.Web.Services.WebService
{
    #region Service
    public Service()
    {
        //Uncomment the following line if using designed components 
        //InitializeComponent();
    }
    string ErrorMSG;
    string UserName;
    public void setLog(string Command, string OfficeSpaceId, string ObjectId, string ItemId, string strxml, string ColumnData, string WhereData, string sErrorMSG)
    {
        if (UserName == "") return;
        //string Folder = Server.MapPath("Log/" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year);
        string Folder = Server.MapPath("Log/" + OfficeSpaceId);
        if (!Directory.Exists(Folder))
            Directory.CreateDirectory(Folder);
        Folder = Folder + "/" + ObjectId;
        if (!Directory.Exists(Folder))
            Directory.CreateDirectory(Folder);
        Folder = Folder + "/" + ItemId;
        if (!Directory.Exists(Folder))
            Directory.CreateDirectory(Folder);
        Folder = Folder + "/" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year;
        if (!Directory.Exists(Folder))
            Directory.CreateDirectory(Folder);
        Folder = Folder + "/" + UserName;
        if (!Directory.Exists(Folder))
            Directory.CreateDirectory(Folder);
        string sData = @"
<Log>
<UserName>@UserName</UserName>
<ID>@ID</ID>
<Command>@Command</Command>
<OfficeSpaceId>@OfficeSpaceId</OfficeSpaceId>
<ObjectId>@ObjectId</ObjectId>
<ItemId>@ItemId</ItemId>
<ErrorMessage>@ErrorMessage</ErrorMessage>
<Data><!--@Data--></Data>
<ColumnData>@ColumnData</ColumnData>
<WhereData>@WhereData</WhereData>
</Log>";
        string ID = DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + "-" + DateTime.Now.Millisecond;
        sData = sData.Replace("@ID", ID);
        sData = sData.Replace("@Command", Command);
        sData = sData.Replace("@OfficeSpaceId", OfficeSpaceId);
        sData = sData.Replace("@ObjectId", ObjectId);
        sData = sData.Replace("@OfficeSpaceId", OfficeSpaceId);
        sData = sData.Replace("@ItemId", ItemId);
        sData = sData.Replace("@Data", strxml);
        sData = sData.Replace("@ColumnData", ColumnData);
        sData = sData.Replace("@WhereData", WhereData);
        sData = sData.Replace("@ErrorMessage", sErrorMSG);
        sData = sData.Replace("@UserName", UserName);
        string LogType = "E";
        if (sErrorMSG == "") LogType = "N";

        XmlDocument xDoc = new XmlDocument();
        xDoc.LoadXml(sData);
        xDoc.Save(Folder + "/" + LogType + "-" + ID + ".xml");

        //File.WriteAllText(Folder + "/" + LogType + "-" + ID + ".txt", WhereData);
    }
    
    [WebMethod]
    public string getError()
    {
        return ErrorMSG;
    }
    [WebMethod]
    public bool setPath(string Password, string PathStore)
    {
        if (Password != "Nextwaver.net")
            return false;

        XmlDocument xConfig = new XmlDocument();
        xConfig.Load(Server.MapPath("Config.xml"));

        xConfig.SelectSingleNode("//Config[@ID='Store']").Attributes["Value"].Value = PathStore;
        xConfig.Save(Server.MapPath("Config.xml"));

        return true;
    }
    [WebMethod]
    public bool setWorkspaceConfig(string Password, string strWorkspace)
    {
        if (Password != "Nextwaver.net")
            return false;

        XmlDocument xConfig = new XmlDocument();
        xConfig.LoadXml(strWorkspace);
        xConfig.Save(Server.MapPath("WorkSpace.xml"));
        return true;
    }
    string GetIP()
    {
        IPHostEntry host;
        string localIP = "?";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily.ToString() == "InterNetwork")
            {
                localIP = ip.ToString();
            }
        }
        return localIP;
    }
    string getPathStore()
    {
        return Server.MapPath("CV");

        //XmlDocument xConfig = new XmlDocument();
        //xConfig.Load(Server.MapPath("Config.xml"));
        //return xConfig.SelectSingleNode("//Config[@ID='Store']").Attributes["Value"].Value;
    }

    [WebMethod]
    public string GetDocumentByVersion(string Connection, string OfficeSpaceId, string DatabaseName, string TableName, string ItemId, string Version)
    {
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            return "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง";
        }

        XmlDocument xFileSystem = new XmlDocument();
        xFileSystem.Load(Server.MapPath("FileSystem.xml"));

        string PathStore = getPathStore();
        int iID = int.Parse(ItemId);
        string ItemIdFolder = Gobals.Methods.GenItemFile(iID);
        string FolderId = xFileSystem.SelectSingleNode("//Item[@Min<=" + int.Parse(ItemId) + "][@Max>=" + int.Parse(ItemId) + "]").Attributes["ID"].Value;
        string ObjectId = "DB-" + DatabaseName + "$TB-" + TableName + "$DOC";

        int iVersion = int.Parse(Version);

        string FolderVersion = xFileSystem.SelectSingleNode("//Item[@Min<=" + int.Parse(Version) + "][@Max>=" + int.Parse(Version) + "]").Attributes["ID"].Value;

        string RootPath = PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion;

        string PathDocLastVersion = RootPath + @"/" + Gobals.Methods.GenItemFile(1) + ".xml";
        string DocumentVersion = File.ReadAllText(PathDocLastVersion);

        for (int i = 2; i <= iVersion; i++)
        {
            string PathTemp = RootPath + @"/" + Gobals.Methods.GenItemFile(i) + ".xml";
            string Diff = File.ReadAllText(PathTemp);

            string newVersionDocument = "";
            bool bError;
            string MsgError;

            Gobals.ControlVersion.PatchXML(DocumentVersion, Diff, Server.MapPath("Temp"), out newVersionDocument, out bError, out MsgError);
            System.Threading.Thread.Sleep(100);
            DocumentVersion = newVersionDocument;
            if (bError)
            {

            }
        }

        return DocumentVersion;
    }
    
    [WebMethod]
    public string GetDocumentVersion(string Connection, string OfficeSpaceId, string DatabaseName, string TableName, string ItemId)
    {
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            return "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง";
        }
        XmlDocument xFileSystem = new XmlDocument();
        xFileSystem.Load(Server.MapPath("FileSystem.xml"));

        string PathStore = getPathStore();
        int iID = int.Parse(ItemId);
        string ItemIdFolder = Gobals.Methods.GenItemFile(iID);
        string FolderId = xFileSystem.SelectSingleNode("//Item[@Min<=" + int.Parse(ItemId) + "][@Max>=" + int.Parse(ItemId) + "]").Attributes["ID"].Value;
        string ObjectId = "DB-" + DatabaseName + "$TB-" + TableName + "$DOC";
        string Version;
        try
        {
            string[] DirList = Directory.GetDirectories(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder);
            string LastVersionFolder = Gobals.Methods.GenFolderId(DirList.Length);
            string[] filList = Directory.GetFiles(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + LastVersionFolder);

            Version = "" + (((DirList.Length - 1) * 2000) + (filList.Length));
        }
        catch { Version = "0"; }

        return Version;
    }
    [WebMethod]
    public string[] CreateOfficeSpace(string Connection, string OfficeSpaceId)
    {
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            string[] output = { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" };
            return output;
        }
        string strPath = Server.MapPath("Store/" + OfficeSpaceId);
        if (!Directory.Exists(strPath))
        {
            Directory.CreateDirectory(strPath);
            string[] output = { "OK", "สร้าง OfficeSpace เรียบร้อยแล้ว" };
            return output;
        }
        else
        {
            string[] output = { "Error", "มี OfficeSpace นี้อยู่แล้วในระบบ" };
            return output;
        }
    }
    //[WebMethod]
    //public string SaveDocument(string OfficeSpaceId, string ObjectId, string ItemId, string strDocument,out string _Version)
    //{
    //    XmlDocument xFileSystem = new XmlDocument();
    //    xFileSystem.Load(Server.MapPath("FileSystem.xml"));
    //    string PathStore = getPathStore();
    //    string FolderId = xFileSystem.SelectSingleNode("//Item[@Min<=" + int.Parse(ItemId) + "][@Max>=" + int.Parse(ItemId) + "]").Attributes["ID"].Value;

    //    int iID = int.Parse(ItemId);
    //    string FileItem = Gobals.Methods.GenItemFile(iID);

    //    string Version;
    //    try
    //    {
    //        string[] DirList = Directory.GetDirectories(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + ItemId + @"\" + FileItem);
    //        string CV_FolderId = Gobals.Methods.GenFolderId(DirList.Length);
    //        string[] filList = Directory.GetFiles(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + ItemId + @"\" + FileItem + @"\" + CV_FolderId);

    //        Version = "" + (((DirList.Length - 1) * 2000) + (filList.Length + 1));
    //    }
    //    catch { Version = "1"; }       

    //    Gobals.Sockets.TCP_Client TCPC = new Gobals.Sockets.TCP_Client();
    //    string Server_IP = GetIP();

    //    //เริ่มสร้าง ROOT PATH
    //    if (!Directory.Exists(PathStore))
    //        Directory.CreateDirectory(PathStore);
    //    if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId))
    //        Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId);
    //    if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId))
    //        Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId);
    //    //จบการสร้าง ROOT PATH

    //    //เริ่มสร้าง Version Control
    //    if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV"))
    //        Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV");
    //    if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV"))
    //        Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV");
    //    //จบการสร้าง Version Control


    //    if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV\" + ItemId))
    //        Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV\" + ItemId);
    //    if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + ItemId))
    //        Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + ItemId);

    //    _Version = Version;

    //    if (Version != "1")
    //    {
    //        string PathDocLastVersion = PathStore + @"/" + OfficeSpaceId + @"/" + ObjectId + @"/LV/" + ItemId + @"/" + FileItem + ".xml";
    //        string Document_LastVersion = File.ReadAllText(PathDocLastVersion);            

    //        string docHas, strDIff = "", msgError;
    //        bool bError = false;
    //        Gobals.ControlVersion.CreateDiff(Document_LastVersion, strDocument, Server.MapPath(""), out docHas, out strDIff, out bError, out msgError);
    //        if (strDIff == "") return "ERROR:ไม่มีการแก้ไขข้อมูล";
    //        string strTemp = "<?xml version=\"1.0\" encoding=\"windows-874\"?>" +
    //                       "<xd:xmldiff version=\"1.0\" srcDocHash=\"" + docHas + "\" options=\"None\" fragments=\"no\" xmlns:xd=\"http://schemas.microsoft.com/xmltools/2002/xmldiff\" />";
    //        strTemp = strTemp.Replace(" ", "");

    //        if (strDIff.Replace(" ", "") == strTemp) return "ERROR:ไม่มีการแก้ไขข้อมูล";

    //        string FileName = FileItem;
    //        string SaveFileLastVersion = PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV\" + ItemId + @"\" + FileName + ".xml";
    //        XmlDocument xTempp = new XmlDocument();
    //        xTempp.LoadXml(strDocument);
    //        xTempp.Save(SaveFileLastVersion);

    //        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + ItemId + @"\" + FileName))
    //            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + ItemId + @"\" + FileName);

    //        XmlNode nodeItem = xFileSystem.SelectSingleNode("//Item[@Min<=" + Version + "][@Max>=" + Version + "]");
    //        string ItemID = nodeItem.Attributes["ID"].Value;
    //        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + ItemID + @"\" + FileName + @"\" + ItemId))
    //            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + ItemId + @"\" + FileName + @"\" + ItemId);

    //        string FileVersionName = Gobals.Methods.GetVersionFile(Version);
    //        string SaveFileControlVersion = PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + ItemId + @"\" + FileName + @"\" + ItemId + @"\" + FileVersionName + ".xml";


    //        Gobals.Methods.SaveFile(SaveFileControlVersion, strDIff);
    //        if (TCPC.SendData(Gobals.Command.Server.Password, Gobals.Command.OfficeSpace.Document,
    //             Gobals.Message.Server.setDocument(OfficeSpaceId, ObjectId, ItemId, strDIff, Version), Server_IP, OfficeSpaceId, Server.MapPath("WorkSpace.xml")))
    //        {
    //            setLog("SaveDocument", OfficeSpaceId, ObjectId, ItemId, strDocument,"","", "");
    //            return "";
    //        }
    //        else
    //        {
    //            setLog("SaveDocument", OfficeSpaceId, ObjectId, ItemId, strDocument,"","", Gobals.Sockets.TCP_Client.curMsg_client);
    //            return Gobals.Sockets.TCP_Client.curMsg_client;
    //        }
    //    }
    //    else
    //    {
    //        string FileName = FileItem;
    //        string SaveFileLastVersion = PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV\" + ItemId + @"\" + FileName + ".xml";
    //        XmlDocument xTempp = new XmlDocument();
    //        xTempp.LoadXml(strDocument);
    //        xTempp.Save(SaveFileLastVersion);

    //        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + ItemId + @"\" + FileName))
    //            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + ItemId + @"\" + FileName);

    //        XmlNode nodeItem = xFileSystem.SelectSingleNode("//Item[@Min<=" + Version + "][@Max>=" + Version + "]");
    //        string ItemID = nodeItem.Attributes["ID"].Value;
    //        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + ItemId + @"\" + FileName + @"\" + ItemID))
    //            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + ItemId + @"\" + FileName + @"\" + ItemID);

    //        string FileVersionName = Gobals.Methods.GetVersionFile(Version);
    //        string SaveFileControlVersion = PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + ItemId + @"\" + FileName + @"\" + ItemID + @"\" + FileVersionName + ".xml";

    //        xTempp.Save(SaveFileControlVersion);

    //        if (TCPC.SendData(Gobals.Command.Server.Password, Gobals.Command.OfficeSpace.Document,
    //            Gobals.Message.Server.setDocument(OfficeSpaceId, ObjectId, ItemId, strDocument, Version), Server_IP, OfficeSpaceId, Server.MapPath("WorkSpace.xml")))
    //        {
    //            setLog("SaveDocument", OfficeSpaceId, ObjectId, ItemId, strDocument,"","", "");
    //            return "";
    //        }
    //        else
    //        {
    //            setLog("SaveDocument", OfficeSpaceId, ObjectId, ItemId, strDocument,"","", Gobals.Sockets.TCP_Client.curMsg_client);
    //            return Gobals.Sockets.TCP_Client.curMsg_client;
    //        }
    //    }
    //}

    [WebMethod]
    public string SaveDocumentNoSent(string OfficeSpaceId, string ObjectId, string ItemId, string strDocument, out string _Version)
    {
        XmlDocument xFileSystem = new XmlDocument();
        xFileSystem.Load(Server.MapPath("FileSystem.xml"));

        string PathStore = getPathStore();
        int iID = int.Parse(ItemId);
        string ItemIdFolder = Gobals.Methods.GenItemFile(iID);
        string FolderId = xFileSystem.SelectSingleNode("//Item[@Min<=" + int.Parse(ItemId) + "][@Max>=" + int.Parse(ItemId) + "]").Attributes["ID"].Value;

        string Version;
        try
        {
            string[] DirList = Directory.GetDirectories(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder);
            string LastVersionFolder = Gobals.Methods.GenFolderId(DirList.Length);
            string[] filList = Directory.GetFiles(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + LastVersionFolder);

            Version = "" + (((DirList.Length - 1) * 2000) + (filList.Length + 1));

        }
        catch { Version = "1"; }


        _Version = Version;
        Gobals.Sockets.TCP_Client TCPC = new Gobals.Sockets.TCP_Client();
        string Server_IP = GetIP();
        string FolderVersion = xFileSystem.SelectSingleNode("//Item[@Min<=" + int.Parse(Version) + "][@Max>=" + int.Parse(Version) + "]").Attributes["ID"].Value;
        string FileVersionName = Gobals.Methods.GenItemFile(int.Parse(Version));

        //เริ่มสร้าง ROOT PATH
        if (!Directory.Exists(PathStore))
            Directory.CreateDirectory(PathStore);
        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId))
            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId);
        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId))
            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId);
        //จบการสร้าง ROOT PATH

        //เริ่มสร้าง Version Control
        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV"))
            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV");
        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV"))
            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV");
        //จบการสร้าง Version Control

        //เริ่มสร้าง Folder ID
        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV\" + FolderId))
            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV\" + FolderId);
        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId))
            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId);
        //จบการสร้าง Folder ID

        //เริ่มสร้าง Item Folder ID      
        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder))
            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder);
        //จบการสร้าง Item Folder ID

        //เริ่มสร้าง Item Folder Version ID       
        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion))
            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion);
        //จบการสร้าง Item Folder Version ID

        string SaveFileLastVersion = PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV\" + FolderId + @"\" + ItemIdFolder + ".xml";
        string SaveFileControlVersion = PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion + @"\" + FileVersionName + ".xml";

        if (Version != "1")
        {
            string Document_LastVersion = File.ReadAllText(SaveFileLastVersion);
            bool bError = false;
            string outDocument = "", MsgError = "";
            Gobals.ControlVersion.PatchXML(Document_LastVersion, strDocument, Server.MapPath("Temp"), out outDocument, out bError, out MsgError);
            Gobals.Methods.SaveFile(SaveFileLastVersion, outDocument);
            Gobals.Methods.SaveFile(SaveFileControlVersion, strDocument);
            setLog("WorkSpaceVer." + Version, OfficeSpaceId, ObjectId, ItemId, strDocument, "", "", "");
            setLog("LastVersion" + Version, OfficeSpaceId, ObjectId, ItemId, Document_LastVersion, "", "", "");
            return "";
        }
        else
        {

            setLog("WorkSpaceVer." + Version, OfficeSpaceId, ObjectId, ItemId, strDocument, "", "", "");
            //บันทึกข้อมูล Version สุดท้าย
            XmlDocument xTempp = new XmlDocument();
            xTempp.LoadXml(strDocument);
            xTempp.Save(SaveFileLastVersion);
            // จบการบันทึก        
            //บันทึกข้อมูล Control Version สุดท้าย
            xTempp.Save(SaveFileControlVersion);
            // จบการบันทึก    
            return "";
        }
    }

    [WebMethod]
    public string SaveDocument(string OfficeSpaceId, string ObjectId, string ItemId, string strDocument, out string _Version)
    {
        XmlDocument xFileSystem = new XmlDocument();
        xFileSystem.Load(Server.MapPath("FileSystem.xml"));

        string PathStore = getPathStore();
        int iID = int.Parse(ItemId);
        string ItemIdFolder = Gobals.Methods.GenItemFile(iID);
        string FolderId = xFileSystem.SelectSingleNode("//Item[@Min<=" + int.Parse(ItemId) + "][@Max>=" + int.Parse(ItemId) + "]").Attributes["ID"].Value;

        string Version;
        try
        {
            string[] DirList = Directory.GetDirectories(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder);
            string LastVersionFolder = Gobals.Methods.GenFolderId(DirList.Length);
            string[] filList = Directory.GetFiles(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + LastVersionFolder);

            Version = "" + (((DirList.Length - 1) * 2000) + (filList.Length + 1));

        }
        catch { Version = "1"; }

        _Version = Version;
        Gobals.Sockets.TCP_Client TCPC = new Gobals.Sockets.TCP_Client();
        string Server_IP = GetIP();
        string FolderVersion = xFileSystem.SelectSingleNode("//Item[@Min<=" + int.Parse(Version) + "][@Max>=" + int.Parse(Version) + "]").Attributes["ID"].Value;
        string FileVersionName = Gobals.Methods.GenItemFile(int.Parse(Version));

        //เริ่มสร้าง ROOT PATH
        if (!Directory.Exists(PathStore))
            Directory.CreateDirectory(PathStore);
        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId))
            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId);
        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId))
            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId);
        //จบการสร้าง ROOT PATH

        //เริ่มสร้าง Version Control
        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV"))
            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV");
        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV"))
            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV");
        //จบการสร้าง Version Control

        //เริ่มสร้าง Folder ID
        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV\" + FolderId))
            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV\" + FolderId);
        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId))
            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId);
        //จบการสร้าง Folder ID

        //เริ่มสร้าง Item Folder ID      
        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder))
            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder);
        //จบการสร้าง Item Folder ID

        //เริ่มสร้าง Item Folder Version ID       
        if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion))
            Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion);
        //จบการสร้าง Item Folder Version ID

        string SaveFileLastVersion = PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV\" + FolderId + @"\" + ItemIdFolder + ".xml";
        string SaveFileControlVersion = PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion + @"\" + FileVersionName + ".xml";

        if (Version != "1")
        {
            string Document_LastVersion = File.ReadAllText(SaveFileLastVersion);

            string docHas, strDIff = "", msgError;
            bool bError = false;
            Gobals.ControlVersion.CreateDiff(Document_LastVersion, strDocument, Server.MapPath("Temp"), out docHas, out strDIff, out bError, out msgError);
            if (strDIff == "") return "ERROR:ไม่มีการแก้ไขข้อมูล";
            string strTemp = "<?xml version=\"1.0\" encoding=\"windows-874\"?>" +
                           "<xd:xmldiff version=\"1.0\" srcDocHash=\"" + docHas + "\" options=\"None\" fragments=\"no\" xmlns:xd=\"http://schemas.microsoft.com/xmltools/2002/xmldiff\" />";
            strTemp = strTemp.Replace(" ", "");

            if (strDIff.Replace(" ", "") == strTemp) return "ERROR:ไม่มีการแก้ไขข้อมูล";

            XmlDocument xTempp = new XmlDocument();
            xTempp.LoadXml(strDocument);
            xTempp.Save(SaveFileLastVersion);

            Gobals.Methods.SaveFile(SaveFileControlVersion, strDIff);
            return "";
            /*if (TCPC.SendData(Gobals.Command.Server.Password, Gobals.Command.OfficeSpace.Document,
                 Gobals.Message.Server.setDocument(OfficeSpaceId, ObjectId, ItemId, strDIff, Version), Server_IP, OfficeSpaceId, Server.MapPath("WorkSpace.xml")))
            {
                setLog("SaveDocument", OfficeSpaceId, ObjectId, ItemId, strDocument, "", "", "");
                return "";
            }
            else
            {
                setLog("SaveDocument", OfficeSpaceId, ObjectId, ItemId, strDocument, "", "", Gobals.Sockets.TCP_Client.curMsg_client);
                return Gobals.Sockets.TCP_Client.curMsg_client;
            }*/
        }
        else
        {


            //บันทึกข้อมูล Version สุดท้าย
            XmlDocument xTempp = new XmlDocument();
            xTempp.LoadXml(strDocument);
            xTempp.Save(SaveFileLastVersion);
            // จบการบันทึก        
            //บันทึกข้อมูล Control Version สุดท้าย
            xTempp.Save(SaveFileControlVersion);
            // จบการบันทึก    
            return "";
            /*if (TCPC.SendData(Gobals.Command.Server.Password, Gobals.Command.OfficeSpace.Document,
                Gobals.Message.Server.setDocument(OfficeSpaceId, ObjectId, ItemId, strDocument, Version), Server_IP, OfficeSpaceId, Server.MapPath("WorkSpace.xml")))
            {
                setLog("SaveDocument", OfficeSpaceId, ObjectId, ItemId, strDocument, "", "", "");
                return "";
            }
            else
            {
                setLog("SaveDocument", OfficeSpaceId, ObjectId, ItemId, strDocument, "", "", Gobals.Sockets.TCP_Client.curMsg_client);
                return Gobals.Sockets.TCP_Client.curMsg_client;
            }*/
        }
    }
    [WebMethod]
    public string GetDocument_LastVersion(string OfficeSpaceId, string ObjectId, string ItemId)
    {
        try
        {
            XmlDocument xFileSystem = new XmlDocument();
            xFileSystem.Load(Server.MapPath("FileSystem.xml"));
            string PathStore = getPathStore();
            string FolderId = xFileSystem.SelectSingleNode("//Item[@Min<=" + int.Parse(ItemId) + "][@Max>=" + int.Parse(ItemId) + "]").Attributes["ID"].Value;

            int iID = int.Parse(ItemId);
            string FileItem = Gobals.Methods.GenItemFile(iID);

            string Version;
            try
            {
                string[] DirList = Directory.GetDirectories(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + FileItem);
                string CV_FolderId = Gobals.Methods.GenFolderId(DirList.Length);
                string[] filList = Directory.GetFiles(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + FileItem + @"\" + CV_FolderId);

                Version = "" + (((DirList.Length - 1) * 2000) + (filList.Length + 1));
            }
            catch { Version = "1"; }

            string PathDocLastVersion = PathStore + @"/" + OfficeSpaceId + @"/" + ObjectId + @"/LV/" + FolderId + @"/" + FileItem + ".xml";
            string Document_LastVersion = File.ReadAllText(PathDocLastVersion);

            setLog("GetDocument_LastVersion", OfficeSpaceId, ObjectId, ItemId, Document_LastVersion, "", "", "");

            return Document_LastVersion;
        }
        catch (Exception ex)
        {
            setLog("GetDocument_LastVersion", OfficeSpaceId, ObjectId, ItemId, "", "", "", ex.Message);
            return ex.Message;
        }
    }
    [WebMethod]
    public string[] CreateDatabase(string Connection, string OfficeSpaceId, string DatabaseName)
    {
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            string[] output = { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" };
            return output;
        }
        string strPath = Server.MapPath("Store/" + OfficeSpaceId);
        if (!Directory.Exists(strPath))
        {
            string[] output = { "Error", "ไม่พบ OfficeSpaceId ที่ระบุ" };
            return output;
        }
        NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, Server.MapPath(""));
        if (NDB.newDatabase(DatabaseName))
        {
            string[] output = { "OK", "สร้างฐ้านข้อมูลสำเร็จ" };
            return output;
        }
        else
        {
            string[] output = { "Error", NDB.ErrorMsg };
            return output;
        }
    }
    [WebMethod]
    public string[] CreateTable(string Connection, string OfficeSpaceId, string DatabaseName, string TableName)
    {
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            string[] output = { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" };
            return output;
        }
        string strPath = Server.MapPath("Store/" + OfficeSpaceId);
        if (!Directory.Exists(strPath))
        {
            string[] output = { "Error", "ไม่พบ OfficeSpaceId ที่ระบุ" };
            return output;
        }
        NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, Server.MapPath(""));
        if (NDB.newTable(DatabaseName, TableName))
        {
            string[] output = { "OK", "สร้างตารางสำเร็จ" };
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(NDB.OutputXmlFile);
            string ObjectId = "DB-" + DatabaseName + "$TB-" + TableName;
            //ต้องเพิ่ม log file
            try
            {
                string Version;
                SaveDocument(OfficeSpaceId, ObjectId, "1", xDoc.OuterXml, out Version);
            }
            catch (Exception ex)
            {
                setLog("SaveDocument", OfficeSpaceId, ObjectId, "1", xDoc.OuterXml, "", "", ex.Message);
            }
            return output;
        }
        else
        {
            string[] output = { "Error", NDB.ErrorMsg };
            return output;
        }
    }
    [WebMethod]
    public string[] CreateColumn(string Connection, string OfficeSpaceId, string DatabaseName, string TableName, string ColumnName, string Detail, NextwaverDB.NColumnType NCType)
    {
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            string[] output = { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" };
            return output;
        }
        string strPath = Server.MapPath("Store/" + OfficeSpaceId);
        if (!Directory.Exists(strPath))
        {
            string[] output = { "Error", "ไม่พบ OfficeSpaceId ที่ระบุ" };
            return output;
        }
        NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, Server.MapPath(""));
        if (NDB.newColumn(DatabaseName, TableName, ColumnName, Detail, NCType))
        {
            string[] output = { "OK", "สร้างคอลัมภ์สำเร็จ" };
            NextwaverDB.NOutputXMLs NOPX_Update = NDB.NOPXMLS_Update;
            for (int i = 0; i < NOPX_Update._Count; i++)
            {
                NextwaverDB.NOutputXML NOPX = NOPX_Update.get(i);
                string FID = NOPX.ObjectID;
                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(NOPX.strXML);
                string ObjectId = "DB-" + DatabaseName + "$TB-" + TableName;
                //ต้องเพิ่ม log file
                try
                {
                    string Version;
                    SaveDocument(OfficeSpaceId, ObjectId, FID, xDoc.OuterXml, out Version);
                }
                catch (Exception ex)
                {
                    setLog("SaveDocument", OfficeSpaceId, ObjectId, FID, xDoc.OuterXml, "", "", ex.Message);
                }
            }
            return output;
        }
        else
        {
            string[] output = { "Error", NDB.ErrorMsg };
            return output;
        }
    }
    #endregion

    #region NextwaverDB
    [WebMethod]
    public string[] InsertData(string Connection, string OfficeSpaceId, string DatabaseName, string TableName, string NColumns_String, string strDOC, string User)
    {
        UserName = User;
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            string[] output = { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" };
            setLog("InsertData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, "", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง");
            return output;
        }

        NextwaverDB.NColumns NCS = new NextwaverDB.NColumns();
        NCS.strXML = strDOC;
        if (!NCS.ImportString(NColumns_String))
        {
            string[] strList = { "Error", NCS.ErrorMSG };
            setLog("InsertData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, "", NCS.ErrorMSG);
            return strList;
        }

        NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, Server.MapPath(""));

        if (NDB.insert(DatabaseName, TableName, NCS))
        {
            string Version = "", VersionDoc = "";
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(NDB.OutputXmlFile);
            string ObjectId = "DB-" + DatabaseName + "$TB-" + TableName;
            //ต้องเพิ่ม log file
            try
            {
                SaveDocument(OfficeSpaceId, ObjectId, NDB.OutputFileID, xDoc.OuterXml, out Version);
            }
            catch (Exception ex)
            {
                setLog("SaveDocument", OfficeSpaceId, ObjectId, NDB.OutputFileID, xDoc.OuterXml, "", "", ex.Message);
            }
            if (NCS.strXML != "")
            {
                xDoc = new XmlDocument();
                xDoc.LoadXml(NCS.strXML);
                ObjectId = "DB-" + DatabaseName + "$TB-" + TableName + "$DOC";
                try
                {
                    SaveDocument(OfficeSpaceId, ObjectId, NDB.OutputFileID, xDoc.OuterXml, out VersionDoc);
                }
                catch (Exception ex)
                {
                    setLog("SaveDocument", OfficeSpaceId, ObjectId, NDB.OutputFileID, xDoc.OuterXml, "", "", ex.Message);
                }
            }
            setLog("InsertData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, "", "");

            try { Transform(Connection, OfficeSpaceId, DatabaseName, TableName, int.Parse(NDB.NewItemID), User, true); }
            catch { }

            string[] output = { "OK", "เพิ่มข้อมูลเรียบร้อยแล้ว", Version, VersionDoc, NDB.NewItemID, NDB.OutputFileID };
            return output;
        }
        else
        {
            string[] output = { "Error", NDB.ErrorMsg };
            setLog("InsertData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, "", NDB.ErrorMsg);
            return output;
        }
    }
    [WebMethod]
    public string[] UpdateData(string Connection, string OfficeSpaceId, string DatabaseName, string TableName, string NColumns_String, string NWheres_String, string strDOC, string User)
    {
        UserName = User;
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            string[] output = { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" };
            setLog("UpdateData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, NWheres_String, "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง");
            return output;
        }

        NextwaverDB.NColumns NCS = new NextwaverDB.NColumns();
        NCS.strXML = strDOC;
        if (!NCS.ImportString(NColumns_String))
        {
            string[] strList = { "Error", NCS.ErrorMSG };
            setLog("UpdateData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, NWheres_String, NCS.ErrorMSG);
            return strList;
        }
        NextwaverDB.NWheres NWS = new NextwaverDB.NWheres();
        if (!NWS.ImportString(NWheres_String))
        {
            string[] strList = { "Error", NWS.ErrorMSG };
            setLog("UpdateData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, NWheres_String, NCS.ErrorMSG);
            return strList;
        }

        NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, Server.MapPath(""));
        if (NDB.update(DatabaseName, TableName, NCS, NWS))
        {
            string Version = "", VersionDoc = "";
            NextwaverDB.NOutputXMLs NOPX_Update = NDB.NOPXMLS_Update;
            for (int i = 0; i < NOPX_Update._Count; i++)
            {
                NextwaverDB.NOutputXML NOPX = NOPX_Update.get(i);
                string FID = NOPX.ObjectID;
                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(NOPX.strXML);
                string ObjectId = "DB-" + DatabaseName + "$TB-" + TableName;
                //ต้องเพิ่ม log file
                try
                {
                    SaveDocument(OfficeSpaceId, ObjectId, FID, xDoc.OuterXml, out Version);
                }
                catch (Exception ex)
                {
                    setLog("SaveDocument", OfficeSpaceId, ObjectId, FID, xDoc.OuterXml, "", "", ex.Message);
                }
            }
            NextwaverDB.NOutputXMLs NOPX_Doc = NDB.NOPXMLS_Doc;
            for (int i = 0; i < NOPX_Doc._Count; i++)
            {
                NextwaverDB.NOutputXML NOPX = NOPX_Doc.get(i);
                string FID = NOPX.ObjectID;

                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(NOPX.strXML);
                string ObjectId = "DB-" + DatabaseName + "$TB-" + TableName + "$DOC";
                try
                {
                    SaveDocument(OfficeSpaceId, ObjectId, FID, xDoc.OuterXml, out VersionDoc);
                }
                catch (Exception ex)
                {
                    setLog("SaveDocument", OfficeSpaceId, ObjectId, FID, xDoc.OuterXml, "", "", ex.Message);
                }
            }

            try { Transform(Connection, OfficeSpaceId, DatabaseName, TableName, int.Parse(NWS.Get("ID").Value), User, true); }
            catch { }

            setLog("UpdateData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, NWheres_String, "");

            string[] output = { "OK", NDB.OutputMsg, Version, VersionDoc };
            return output;
        }
        else
        {
            string[] output = { "Error", NDB.ErrorMsg };
            setLog("UpdateData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, NWheres_String, NDB.ErrorMsg);
            return output;
        }
    }
    [WebMethod]
    public DataTable SelectByColumnAndWhere(string Connection, string OfficeSpaceId, string DatabaseName, string TableName, string NColumns_encrypt, string NWheres_encrypt, string User)
    {
        string NWheres_String = new EncryptDecrypt.CryptorEngine().Decrypt(NWheres_encrypt, true);
        string NColumns_String = new EncryptDecrypt.CryptorEngine().Decrypt(NColumns_encrypt, true);
        UserName = User;
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            ErrorMSG = "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง";
            setLog("SelectByColumnAndWhere", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, NWheres_String, ErrorMSG);
            return null;
        }
        NextwaverDB.NColumns NCS = new NextwaverDB.NColumns();
        if (!NCS.ImportString(NColumns_String))
        {
            setLog("SelectByColumnAndWhere", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, NWheres_String, "NColumns:" + NCS.ErrorMSG);
            return null;
        }
        NextwaverDB.NWheres NWS = new NextwaverDB.NWheres();
        if (!NWS.ImportString(NWheres_String))
        {
            setLog("SelectByColumnAndWhere", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, NWheres_String, "NWheres:" + NWS.ErrorMSG);
            return null;
        }
        NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, Server.MapPath(""));

        DataTable dt = NDB.select(DatabaseName, TableName, NCS, NWS);
        if (dt == null)
            setLog("SelectByColumnAndWhere", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, NWheres_String, NDB.ErrorMsg);
        else
            setLog("SelectByColumnAndWhere", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, NWheres_String, "");
        return dt;
    }
    [WebMethod]
    public DataTable SelectAllColumnByWhere(string Connection, string OfficeSpaceId, string DatabaseName, string TableName, string NWheres_String, string User)
    {
        UserName = User;
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            ErrorMSG = "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง";
            setLog("SelectAllColumnByWhere", OfficeSpaceId, DatabaseName, TableName, "", "", NWheres_String, ErrorMSG);
            return null;
        }
        NextwaverDB.NWheres NWS = new NextwaverDB.NWheres();
        if (!NWS.ImportString(NWheres_String))
        {
            setLog("SelectAllColumnByWhere", OfficeSpaceId, DatabaseName, TableName, "", "", NWheres_String, "NWheres:" + NWS.ErrorMSG);
            return null;
        }
        NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, Server.MapPath(""));
        DataTable dt = NDB.select(DatabaseName, TableName, NWS);
        if (dt == null)
            setLog("SelectAllColumnByWhere", OfficeSpaceId, DatabaseName, TableName, "", "", NWheres_String, NDB.ErrorMsg);
        else
            setLog("SelectAllColumnByWhere", OfficeSpaceId, DatabaseName, TableName, "", "", NWheres_String, "");
        return dt;
    }
    [WebMethod]
    public DataTable SelectAllByColumn(string Connection, string OfficeSpaceId, string DatabaseName, string TableName, string NColumns_String, string User)
    {
        UserName = User;
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            ErrorMSG = "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง";
            setLog("SelectAllByColumn", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, "", ErrorMSG);
            return null;
        }
        NextwaverDB.NColumns NCS = new NextwaverDB.NColumns();
        if (!NCS.ImportString(NColumns_String))
        {
            setLog("SelectAllByColumn", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, "", "NColumns:" + ErrorMSG);
            return null;
        }
        NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, Server.MapPath(""));
        DataTable dt = NDB.select(DatabaseName, TableName, NCS);
        if (dt == null)
            setLog("SelectAllByColumn", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, "", NDB.ErrorMsg);
        else
            setLog("SelectAllByColumn", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, "", "");
        return dt;
    }
    [WebMethod]
    public DataTable SelectAll(string Connection, string OfficeSpaceId, string DatabaseName, string TableName, string User)
    {
        UserName = User;
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            ErrorMSG = "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง";
            return null;
        }
        NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, Server.MapPath(""));
        DataTable dt = NDB.select(DatabaseName, TableName);
        if (dt == null)
            setLog("SelectAll", OfficeSpaceId, DatabaseName, TableName, "", "", "", NDB.ErrorMsg);
        else
            setLog("SelectAll", OfficeSpaceId, DatabaseName, TableName, "", "", "", "");
        return dt;
    }
    [WebMethod]
    public string[] updateTable(string Connection, string OfficeSpaceId, string ItemId, string DatabaseName, string TableName, string strData, string User)
    {
        UserName = User;
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            string[] output = { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" };
            setLog("updateTable", OfficeSpaceId, DatabaseName, TableName, strData, "", "", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง");
            return output;
        }

        string strPath = Server.MapPath("Store/" + OfficeSpaceId);
        if (!Directory.Exists(strPath))
        {
            string[] output = { "Error", "ไม่พบ OfficeSpaceId ที่ระบุ" };
            setLog("updateTable", OfficeSpaceId, DatabaseName, TableName, strData, "", "", "ไม่พบ OfficeSpaceId ที่ระบุ");
            return output;
        }
        try
        {
            strPath = strPath + "/database/" + DatabaseName;
            if (!Directory.Exists(strPath))
                Directory.CreateDirectory(strPath);

            strPath = strPath + "/" + TableName;
            if (!Directory.Exists(strPath))
                Directory.CreateDirectory(strPath);

            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, Server.MapPath(""));
            string FileID = NDB.getFileID(int.Parse(ItemId));
            XmlDocument xTempp = new XmlDocument();
            xTempp.LoadXml(strData);
            xTempp.Save(strPath + "/" + FileID + ".xml");

            string[] soutput = { "OK", "แก้ไขตารางเรียบร้อยแล้ว" };
            setLog("updateTable", OfficeSpaceId, DatabaseName, TableName, strData, "", "", "");
            return soutput;
        }
        catch (Exception ex)
        {
            string[] output = { "Error", "EXC:ITEM-" + ItemId + "_" + ex.Message };
            setLog("updateTable", OfficeSpaceId, DatabaseName, TableName, strData, "", "", ex.Message);
            return output;
        }

    }
    [WebMethod]
    public string SelectLastDocument(string Connection, string OfficeSpaceId, string DatabaseName, string TableName, int ItemId, string User)
    {
        UserName = User;
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            ErrorMSG = "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง";
            setLog("SelectLastDocument", OfficeSpaceId, DatabaseName, TableName, "", "", "", "[ID=" + ItemId + "]" + ErrorMSG);
            return "";
        }

        NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, Server.MapPath(""));
        string OutputXML = "";
        if (NDB.selectLastDoc(DatabaseName, TableName, ItemId, out OutputXML))
        {
            ErrorMSG = "";
            setLog("SelectLastDocument", OfficeSpaceId, DatabaseName, TableName, "", "", "", "");
            return OutputXML;
        }
        else
        {
            ErrorMSG = NDB.ErrorMsg;
            setLog("SelectLastDocument", OfficeSpaceId, DatabaseName, TableName, "", "", "", "[ID=" + ItemId + "]" + ErrorMSG);
            return "";
        }
    }
    #endregion

    #region File Controlversion
    [WebMethod]
    public string[] getRootCV(string Connection)
    {
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            string[] output = { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" };
            return output;
        }
        return Directory.GetDirectories(getPathStore());
    }
    [WebMethod]
    public string getDirctoryInfo(string Connection, string Path)
    {
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            return "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง";
        }
        DirectoryInfo DIF = new DirectoryInfo(Path);


        return DIF.Name + "(" + DIF.LastWriteTime.Day + "/" + DIF.LastWriteTime.Month + "/" + DIF.LastWriteTime.Year + ")";
    }
    [WebMethod]
    public string[] getDirctoryList(string Connection, string Path)
    {
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            string[] output = { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" };
            return output;
        }
        return Directory.GetDirectories(Path);
    }
    [WebMethod]
    public string[] getFileList(string Connection, string Path)
    {
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            string[] output = { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" };
            return output;
        }
        return Directory.GetFiles(Path);
    }
    [WebMethod]
    public string getFileName(string Connection, string FilePath)
    {
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            return "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง";
        }

        FileInfo FIF = new FileInfo(FilePath);
        return FIF.Name;
    }
    #endregion

    #region Process
    [WebMethod]
    public bool Transform(string Connection, string OfficeSpaceId, string DatabaseName, string TableName, int ItemId, string User, bool isTransformDoc)
    {
        if (Connection != ConfigurationManager.AppSettings["Connection"])
        {
            string[] output = { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" };
            setLog("Transform", OfficeSpaceId, DatabaseName, TableName, "" + ItemId, "", "", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง");
            ErrorMSG = "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง";
            return false;
        }

        XmlDocument xmlTemp = new XmlDocument();
        xmlTemp.Load(Server.MapPath("Config/Process.xml"));

        XmlNode Process = xmlTemp.SelectSingleNode("//Process[@OFSID='" + OfficeSpaceId + "'][@WSTable='" + TableName + "']");

        if (Process == null)
        {
            ErrorMSG = "ไม่พบคำสั่ง Transform";
            return false;
        }

        setLog("Transform", OfficeSpaceId, DatabaseName, TableName, ItemId.ToString(), "", "", "Start");

        string Sql_Delete = Process.SelectSingleNode("./Query/Delete").InnerText;
        string Sql_Insert = Process.SelectSingleNode("./Query/Insert").InnerText;
        XmlNodeList listColumn = Process.SelectNodes("./Columns/Column");
        string DBConnectionID = Process.Attributes["DBConnectionID"].Value;

        XmlNode nodeDBConnection = xmlTemp.SelectSingleNode("//Config/Connection/Item[@ID='" + DBConnectionID + "']");
        string DBServer = nodeDBConnection.Attributes["Server"].Value;
        string DBDatabase = nodeDBConnection.Attributes["Database"].Value;
        string DBLogin = nodeDBConnection.Attributes["Login"].Value;
        string DBPassword = nodeDBConnection.Attributes["Password"].Value;
        string ConnectoinString = "Data Source=" + DBServer + "; uid=" + DBLogin + "; pwd=" + DBPassword + "; Initial Catalog=" + DBDatabase + ";";

        NextwaverDB.NWheres NWS = new NextwaverDB.NWheres();
        NWS.Add(new NextwaverDB.NWhere("ID", ItemId.ToString()));

        DataTable dt = SelectAllColumnByWhere(Connection, OfficeSpaceId, DatabaseName, TableName, NWS.ExportString(), User);
        List<string> Sql_List = new List<string>();

        for (int k = 0; k < dt.Rows.Count; k++)
        {
            string TempInsert = Sql_Insert;
            string TempDelete = Sql_Delete;
            DataRow DR = dt.Rows[k];

            string strXML = "";
            for (int j = 0; j < listColumn.Count; j++)
            {
                //<Column Type="STR" Name="ID" Parameter="@ID@" />
                string TempType = "" + listColumn[j].Attributes["Type"].Value;
                string TempName = "" + listColumn[j].Attributes["Name"].Value;
                string TempParameter = "" + listColumn[j].Attributes["Parameter"].Value;
                string TempValue = "";
                try
                {
                    TempValue = "" + DR[TempName];
                }
                catch { }
                TempDelete = TempDelete.Replace(TempParameter, TempValue);

                switch (TempType)
                {
                    case "STR":
                        TempInsert = TempInsert.Replace(TempParameter, TempValue);
                        break;
                    case "XML":
                        strXML = SelectLastDocument(Connection, OfficeSpaceId, DatabaseName, TableName, ItemId, User);
                        TempInsert = TempInsert.Replace(TempParameter, strXML);
                        break;
                }
            }

            Sql_List.Add(TempDelete);
            Sql_List.Add(TempInsert);

            // Transform Document
            if (isTransformDoc)
            {
                XmlNodeList listItemDocument = Process.SelectNodes("./Document/Items");
                if (listItemDocument.Count != 0)
                {
                    if (strXML == "")
                        strXML = SelectLastDocument(Connection, OfficeSpaceId, DatabaseName, TableName, ItemId, User);

                    XmlDocument xTemp = new XmlDocument();
                    xTemp.LoadXml(strXML);
                    for (int j = 0; j < listItemDocument.Count; j++)
                    {
                        string ItmName = listItemDocument[j].Attributes["Name"].Value;
                        string ItmType = listItemDocument[j].Attributes["Type"].Value;
                        string ItmSql_Delete = listItemDocument[j].SelectSingleNode("./Query/Delete").InnerText;
                        string ItmSql_Insert = listItemDocument[j].SelectSingleNode("./Query/Insert").InnerText;
                        XmlNodeList ItmlistColumn = listItemDocument[j].SelectNodes("./Columns/Column");

                        XmlNode nodeRealData = xTemp.SelectSingleNode("//Items[@Name='" + ItmName + "']");

                        ItmSql_Delete = ItmSql_Delete.Replace("@ID@", "" + ItemId);
                        Sql_List.Add(ItmSql_Delete);

                        if (ItmType.ToUpper() == "FIX")
                        {
                            for (int w = 0; w < ItmlistColumn.Count; w++)
                            {
                                string TempName = "" + ItmlistColumn[w].Attributes["Name"].Value;
                                string TempParameter = "" + ItmlistColumn[w].Attributes["Parameter"].Value;
                                try
                                {
                                    XmlNode nodeTEMPPP = nodeRealData.SelectSingleNode("./Item[@Name='" + TempName + "']");
                                    string TempValue = nodeTEMPPP.Attributes["Value"].Value;
                                    ItmSql_Insert = ItmSql_Insert.Replace(TempParameter, TempValue);
                                }
                                catch { }
                            }

                            ItmSql_Insert = ItmSql_Insert.Replace("@ID@", "" + ItemId);
                            Sql_List.Add(ItmSql_Insert);
                        }
                        else
                        {
                            XmlNode nodeMeans = nodeRealData.SelectSingleNode("./Means");
                            XmlNodeList listItmSub = nodeRealData.SelectNodes("./Item");

                            for (int h = 0; h < listItmSub.Count; h++)
                            {
                                string ItmSql_InsertSub = ItmSql_Insert;
                                for (int w = 0; w < ItmlistColumn.Count; w++)
                                {
                                    string TempName = "" + ItmlistColumn[w].Attributes["Name"].Value;
                                    string TempParameter = "" + ItmlistColumn[w].Attributes["Parameter"].Value;
                                    try
                                    {
                                        string KeyID = nodeMeans.SelectSingleNode("./Mean[@Name='" + TempName + "']").Attributes["ID"].Value;
                                        string TempValue = listItmSub[h].Attributes[KeyID].Value;
                                        ItmSql_InsertSub = ItmSql_InsertSub.Replace(TempParameter, TempValue);
                                    }
                                    catch { }
                                }

                                try
                                {
                                    XmlNodeList GobalColumns = listItemDocument[j].SelectNodes("./GobalColumns/Column");
                                    string GobalColumns_Name = listItemDocument[j].SelectSingleNode("./GobalColumns").Attributes["Name"].Value;

                                    XmlNode nodeRealGobalData = xTemp.SelectSingleNode("//Items[@Name='" + GobalColumns_Name + "']");
                                    for (int w = 0; w < GobalColumns.Count; w++)
                                    {
                                        string TempName = "" + GobalColumns[w].Attributes["Name"].Value;
                                        string TempParameter = "" + GobalColumns[w].Attributes["Parameter"].Value;
                                        try
                                        {
                                            XmlNode nodeTEMPPP = nodeRealGobalData.SelectSingleNode("./Item[@Name='" + TempName + "']");
                                            string TempValue = nodeTEMPPP.Attributes["Value"].Value;
                                            ItmSql_InsertSub = ItmSql_InsertSub.Replace(TempParameter, TempValue);
                                        }
                                        catch { }
                                    }
                                }
                                catch { }

                                ItmSql_InsertSub = ItmSql_InsertSub.Replace("@ID@", "" + ItemId);
                                Sql_List.Add(ItmSql_InsertSub);
                            }
                        }
                    }
                }
            }
        }

        ConnectServer cConn = new ConnectServer();
        if (cConn.Execute(Sql_List.ToArray(), ConnectoinString))
        {
            setLog("Transform", OfficeSpaceId, DatabaseName, TableName, ItemId.ToString(), "", "", "");
            return true;
        }
        else
        {
            setLog("Transform", OfficeSpaceId, DatabaseName, TableName, ItemId.ToString(), "", "", cConn._ErrorLog);
            return false;
        }
    }
    #endregion
}