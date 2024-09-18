#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.SQLiteStore;
using FTOptix.HMIProject;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.Store;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.NetLogic;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.CoreBase;
using FTOptix.Core;
using System.Diagnostics;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
#endregion

public class RecipeEditorGeneral_RuntimeNetLogic : BaseNetLogic
{


    DelayedTask delayTask;

    ComboBox cmbSchema;
    ComboBox cmbName;
    DataGrid rcpGrid;

    RecipeSchema curSchema = null;
    string curName = string.Empty;
    IUAVariable curEditModel = null;
    IUANode emptyEditModel = null;
    IUANode actualEditModel = null;
    IUAVariable vMessageNo = null;
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started

        delayTask = new DelayedTask(DelayTaskHandle, 1000, LogicObject);


        rcpGrid = LogicObject.GetAlias("Grid") as DataGrid;

        cmbSchema = LogicObject.GetAlias("CmbSchema") as ComboBox;
        if (cmbSchema == null )
        {
            throw new NullReferenceException("Combox Schema is null");
        }


        cmbName = LogicObject.GetAlias("CmbName") as ComboBox;
        if (cmbName == null)
        {
            throw new NullReferenceException("Combox Name is null");
        }




        actualEditModel = LogicObject.GetObject("ActualEditModel");


        vMessageNo = LogicObject.GetVariable("MessageNo");

        //curSchema = GetRecipeShema(cmbSchema.SelectedItem);
        //cmbName.Refresh();
        //cmbName.Text = string.Empty;
        //delayTask.Start();
        RecipeShemaChanged(cmbSchema.SelectedItem);

    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        delayTask.Dispose();
    }


    #region UI EventHandle
    [ExportMethod]
    public void NewRecipe(string name)
    {
        SaveToDB(name,CopyErrorPolicy.StrictCopy);
        SelectDataFromDatabase(name);

    }

    [ExportMethod]
    public void RemoveRecipe()
    {
        
        DeleteRecipe(cmbName.Text);
        cmbName.Refresh();
        cmbName.Text = string.Empty ;
        ClearEditModel();

    }

    [ExportMethod]
    public void SaveRecipe()
    {
        
        SaveToDB(curName,CopyErrorPolicy.StrictCopy);
    }

    [ExportMethod]
    public void DownloadRecipe()
    {
        ApplyToPLC(CopyErrorPolicy.StrictCopy);
    }

    [ExportMethod]
    public void UploadRecipe()
    {
        LoadFromPLC(CopyErrorPolicy.StrictCopy);
    }


    [ExportMethod]
    public void RecipeShemaChanged(NodeId shemaId)
    {
        curSchema = GetRecipeShema(shemaId);
        cmbName.Refresh();
        cmbName.Text = string.Empty;

        ClearEditModel();
        delayTask.Start();
    }

    [ExportMethod]
    public void RecipeNameChanged(string name) {
        Log.Info(name);
        SelectDataFromDatabase(name);

    }


    #endregion


    #region Private Methods

    private IUANode GetSchemaTargetNode(RecipeSchema schema)
    {
        var targetNode = schema.GetVariable("TargetNode");
        if (targetNode == null)
            throw new Exception("Target Node variable not found in schema " + schema.BrowseName);

        if ((NodeId)targetNode.Value == NodeId.Empty)
            throw new Exception("Target Node variable not set in schema " + schema.BrowseName);

        var target = InformationModel.Get(targetNode.Value);
        if (target == null)
            throw new Exception("Target " + targetNode.Value + " not found");

        return target;
    }

    private IUANode GetSchemaRoot(RecipeSchema schema)
    {
        var targetNode = schema.Get("Root");

        return targetNode;
    }

    private void UpdateEditModel(IUANode model)
    {

        foreach(var item in actualEditModel.Children)
        {
            item.Delete();
        }

        var schema = model.Owner as RecipeSchema;
        if(schema != null)
        {
            var targetNode = GetSchemaTargetNode(schema);
            


            CopyEditModelItem(targetNode, model,actualEditModel, string.Empty);
        }

        this.rcpGrid.Refresh();
    }


    [Obsolete]
    private void CopyEditModelItem__(IUANode rootNode,IUANode targetNode,string path)
    {
        //foreach (var item in rootNode.Children)
        //{
        //    if (item as IUAObject != null)
        //    {
        //        CopyEditModelItem(item,targetNode, path + "/" + item.BrowseName);
        //    }
        //    else if(item as IUAVariable != null)
        //    {
        //        var v = item as IUAVariable;

        //        var nv = InformationModel.MakeVariable(path + "/" + v.BrowseName,OpcUa.DataTypes.BaseDataType);
        //        nv.SetDynamicLink(v, DynamicLinkMode.ReadWrite);
        //        targetNode.Add(nv);

        //    }

        //    else
        //    {
        //        continue;
        //    }
        //}
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="editModel">Recipe Schema EditModel</param>
    /// <param name="actualEditModel">Actual EditModel</param>
    /// <param name="path"></param>
    private void CopyEditModelItem(IUANode targetNode, IUANode editModel, IUANode actualEditModel, string path)
    {
        foreach (var item in editModel.Children)
        {
            try
            {

                if (item as IUAObject != null)
                {

                    var _targetNode = targetNode.Get(item.BrowseName);
                    if(_targetNode == null)
                    {
                        Log.Error("child target node miss");
                        continue;
                    }
                    CopyEditModelItem(_targetNode, item, actualEditModel, path + "/" + item.BrowseName);
                }
                else if (item as IUAVariable != null)
                {
                    var v = item as IUAVariable;

                    var _aliasName = path + "/" + v.BrowseName;
                    var nv = InformationModel.MakeVariable(_aliasName,OpcUa.DataTypes.NodeId);
                    nv.Value = item.NodeId;


                    var _targetNode = targetNode.Get(item.BrowseName);
                    if (_targetNode == null)
                    {
                        Log.Error("child target node miss");

                    }
                    else
                    {
                        var _range = GetVariableRange(_targetNode);
                        if (_range != null)
                        {

                            var r = InformationModel.MakeObject("EURange");

                            var vlow = InformationModel.MakeVariable("Low", OpcUa.DataTypes.Double);
                            vlow.SetDynamicLink(_range.LowVariable);
                            r.Add(vlow);

                            var vhigh = InformationModel.MakeVariable("High", OpcUa.DataTypes.Double);
                            vhigh.SetDynamicLink(_range.HighVariable);
                            r.Add(vhigh);
                            //var vlow = InformationModel.MakeVariable("Low", OpcUa.DataTypes.Double);
                            //vlow.SetDynamicLink(_range.LowVariable);
                            //item.Add(vlow);

                            //var vhigh = InformationModel.MakeVariable("High", OpcUa.DataTypes.Double);
                            //vhigh.SetDynamicLink(_range.LowVariable);
                            item.Add(r);



                        }
                    }

                    //targetNode.SetAlias(_aliasName, item);
                    actualEditModel.Add(nv);
                    //var nv = InformationModel.MakeVariable(path + "/" + v.BrowseName, OpcUa.DataTypes.BaseDataType);
                    //nv.SetDynamicLink(v, DynamicLinkMode.ReadWrite);

                }
                else
                {
                    continue;
                }

            }catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
    }


    private FTOptix.Core.Range GetVariableRange(IUANode node)
    {
        var range = node.Get<FTOptix.Core.Range>("EURange");
        if(range != null)
        {
            return range;
        }
        return null;
    }



    private IUANode GetTargetNode(RecipeSchema schema)
    {
        var targetNode = schema.GetVariable("TargetNode");
        if (targetNode == null)
            throw new Exception("Target Node variable not found in schema " + schema.BrowseName);

        if ((NodeId)targetNode.Value == NodeId.Empty)
            throw new Exception("Target Node variable not set in schema " + schema.BrowseName);

        var target = InformationModel.Get(targetNode.Value);
        if (target == null)
            throw new Exception("Target " + targetNode.Value + " not found");

        return target;
    }
    private RecipeSchema GetRecipeShema(NodeId nodeId)
    {
        if(nodeId == null || nodeId == NodeId.Empty)
        {
            return null;
        }
        else
        {
            var s = InformationModel.Get(nodeId) as RecipeSchema;
            if (s != null)
            {
                Log.Info(s.BrowseName);
                return s;
            }else
            {
                return null;
            }
        }
    }

    private void SelectDataFromDatabase(string name)
    {
        curName = string.Empty;
        if(curSchema == null)
        {
            ClearEditModel();
            return;
        }
        if(string.IsNullOrWhiteSpace(name))
        {
            ClearEditModel();
            return;
        }

      

        var editModelNode = curSchema.GetObject("EditModel");
        if (editModelNode == null)
        {
            ClearEditModel();
            return;
        }
        CopyErrorPolicy ErrorPolicy = CopyErrorPolicy.StrictCopy;
        try
        {
            curSchema.CopyFromStoreRecipe(name, editModelNode.NodeId, ErrorPolicy);
            curName = name;

            UpdateEditModel(editModelNode);
            SetFeedback(FeedbackMessageType.OpenRecipeOk);

        }
        catch (Exception ex)
        {
            ClearEditModel();
            SetFeedback(FeedbackMessageType.OpenRecipeError);

        }
       

    }

    private void SaveToDB(string RecipeName, CopyErrorPolicy ErrorPolicy)
    {


        RecipeSchema schema = curSchema;
        if (schema == null) return;

        string name = RecipeName;
        if (String.IsNullOrEmpty(RecipeName))
        {
                return;
        }

        try
        {
            var store = GetSchemaStore(curSchema.Store);
            var editModel = GetEditModel(curSchema);

            if (store != null && editModel != null)
            {
                if (RecipeExistsInStore(store, schema, name))
                {
                    // Save Recipe
                    schema.CopyToStoreRecipe(editModel.NodeId, name, ErrorPolicy);
                    //SetFeedback(1, $"{GetLocalizedTextString("RecipeControllerRecipe")} {name} {GetLocalizedTextString("RecipeControllerSaved")}");
                    SetFeedback(FeedbackMessageType.SaveRecipeOK);
                }
                else
                {
                    // Create recipe
                    schema.CreateStoreRecipe(name);
                    // Save Recipe
                    schema.CopyToStoreRecipe(editModel.NodeId, name, ErrorPolicy);
                    //SetFeedback(1, $"{GetLocalizedTextString("RecipeControllerRecipe")} {name} {GetLocalizedTextString("RecipeControllerCreatedAndSaved")}");
                    SetFeedback(FeedbackMessageType.NewRecipeOK);
                }
            }
        }
        catch (Exception e)
        {
            //SetFeedback(2, e.Message);
            SetFeedback(FeedbackMessageType.StoreRecipeError);
        }
    }
    private void DeleteRecipe(string RecipeName)
    {


        RecipeSchema schema = curSchema;
        if (schema == null) return;
        
        string name = RecipeName;
        if (String.IsNullOrEmpty(RecipeName))
        {
            name = curName;
            if (String.IsNullOrEmpty(name))
            {
                //SetFeedback(2, GetLocalizedTextString("RecipeControllerEmptyRecipeName"));
                return;
            }
        }

        try
        {
            schema.DeleteStoreRecipe(name);
            SetFeedback(FeedbackMessageType.DeleteRecipeOK);
            //SetFeedback(1, $"{GetLocalizedTextString("RecipeControllerRecipe")} {name} {GetLocalizedTextString("RecipeControllerDeleted")}");
        }
        catch (Exception e)
        {
            //SetFeedback(2, e.Message);
            SetFeedback(FeedbackMessageType.DeleteRecipeError);
        }
    }

    private void LoadFromPLC(CopyErrorPolicy ErrorPolicy)
    {

        RecipeSchema schema = curSchema;
        if (schema == null) return;

        try
        {
            var editModel = GetEditModel(schema);
            if (editModel != null)
            {
                schema.Copy(schema.TargetNode, editModel.NodeId, ErrorPolicy);
                //SetFeedback(1, GetLocalizedTextString("RecipeControllerRecipeLoaded"));
                SetFeedback(FeedbackMessageType.UploadRecipeOK);
            }
        }
        catch (Exception e)
        {
            //SetFeedback(2, e.Message);
            SetFeedback(FeedbackMessageType.UploadRecipeError);
        }
    }

    private void ApplyToPLC(CopyErrorPolicy ErrorPolicy)
    {


        RecipeSchema schema = curSchema;
        if (schema == null) return;

        try
        {
            var editModel = GetEditModel(schema);
            if (editModel != null)
            {
                schema.CopyFromEditModel(editModel.NodeId, schema.TargetNode, ErrorPolicy);
                //SetFeedback(1, GetLocalizedTextString("RecipeControllerRecipeApplied"));
                SetFeedback(FeedbackMessageType.DownloadRecipeOk);
            }
        }
        catch (Exception e)
        {
            //SetFeedback(2, e.Message);
            SetFeedback(FeedbackMessageType.DownloadRecipeError);
        }
    }



    private void DelayTaskHandle()
    {
        var name = (string)cmbName.SelectedValue;
        if (string.IsNullOrEmpty(name))
        {
            Log.Info("recipe name is empty");
        }
        else
        {
            Log.Info($"recipe name is {name}");
        }
        SelectDataFromDatabase(name);
        
    }

    private IUANode GetEditModel(RecipeSchema schema)   
    {
        var editModel = schema.Get("EditModel");
        if (editModel == null)
        {
            //SetFeedback(2, $"{GetLocalizedTextString("RecipesEditorEditModelOfSchema")} {schema.BrowseName} {GetLocalizedTextString("RecipesEditorNotFound")}");

        }

        return editModel;

    }

    private Store GetSchemaStore(NodeId nodeId)
    {
        if(nodeId == null)
        {
            return null;
        }
        
        return InformationModel.Get(nodeId) as Store;
    }

    private bool RecipeExistsInStore(FTOptix.Store.Store store, RecipeSchema schema, string recipeName)
    {
        // Perform query on the store in order to check if the recipe already exists
        object[,] resultSet;
        string[] header;
        var tableName = !String.IsNullOrEmpty(schema.TableName) ? schema.TableName : schema.BrowseName;
        store.Query("SELECT * FROM \"" + tableName + "\" WHERE Name LIKE \'" + recipeName + "\'", out header, out resultSet);
        var rowCount = resultSet != null ? resultSet.GetLength(0) : 0;
        return rowCount > 0;
    }

    private void ClearEditModel()
    {
        foreach(var item in actualEditModel.Children)
        {
            item.Delete();
        }

        this.rcpGrid.Refresh();
    }



    private void SetFeedback(FeedbackMessageType feedback)
    {
        vMessageNo.Value = (int)feedback;
    }

    #endregion


    enum FeedbackMessageType
    {
        None = 0,
        NewRecipeOK = 1,
        NewRecipeError = 2,
        SaveRecipeOK = 3,
        StoreRecipeError= 4,
        DeleteRecipeOK = 5,
        DeleteRecipeError = 6,
        UploadRecipeOK = 7,
        UploadRecipeError = 8,

        DownloadRecipeOk = 9,
        DownloadRecipeError = 10,

        OpenRecipeOk = 11,
        OpenRecipeError = 12,

    }
    

}
