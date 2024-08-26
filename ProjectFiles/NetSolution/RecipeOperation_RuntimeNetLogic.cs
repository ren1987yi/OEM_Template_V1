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
#endregion

public class RecipeOperation_RuntimeNetLogic : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }


    #region UI Method
    [ExportMethod]
    public void Download(NodeId schemaId,string recipeName,out int result)
    {

        result = DownloadToPLC(schemaId,recipeName);
    }

    [ExportMethod]
    public void Upload(NodeId schemaId, string recipeName,out int result)
    {
        result = UploadFromPLC(schemaId, recipeName);   
    }


    private int DownloadToPLC(NodeId schemaId,string rcpName)
    {
        var schema = GetRecipeShema(schemaId);
        if (schema == null)
        {
            Log.Error("recipe schema is not exists!");
            
            return -1;
        }

        if (SelectDataFromDatabase(schema, rcpName))
        {
            if(ApplyToPLC(schema, CopyErrorPolicy.StrictCopy))
            {
                
                return 1;
            }
            else
            {
                Log.Error("recipe download error");
                return -1;
            }
        }
        else
        {
            
            Log.Error("recipe open error");
            return -1;
        }


    }

    private int UploadFromPLC(NodeId schemaId,string rcpName)
    {
        var schema = GetRecipeShema(schemaId);
        if (schema == null)
        {
            Log.Error("recipe schema is not exists!");

            return -1;
        }

        if (SelectDataFromDatabase(schema, rcpName))
        {
            if (LoadFromPLC(schema, CopyErrorPolicy.StrictCopy))
            {
                if (SaveToDB(schema, rcpName, CopyErrorPolicy.StrictCopy))
                {

                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            else
            {

                Log.Error("recipe download error");
                return -1;
            }
        }
        else
        {

            Log.Error("recipe open error");
            return -1;
        }
    }



    #endregion
    private bool ApplyToPLC(RecipeSchema curSchema, CopyErrorPolicy ErrorPolicy)
    {


        RecipeSchema schema = curSchema;
        if (schema == null)
        {
            return false;
        }

        try
        {
            var editModel = GetEditModel(schema);
            if (editModel != null)
            {
                schema.CopyFromEditModel(editModel.NodeId, schema.TargetNode, ErrorPolicy);
                //SetFeedback(1, GetLocalizedTextString("RecipeControllerRecipeApplied"));
                //SetFeedback(FeedbackMessageType.DownloadRecipeOk);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            //SetFeedback(2, e.Message);
            //SetFeedback(FeedbackMessageType.DownloadRecipeError);
            return false;
        }
    }

    private bool LoadFromPLC(RecipeSchema curSchema, CopyErrorPolicy ErrorPolicy)
    {

        RecipeSchema schema = curSchema;
        if (schema == null) return false;

        try
        {
            var editModel = GetEditModel(schema);
            if (editModel != null)
            {
                schema.Copy(schema.TargetNode, editModel.NodeId, ErrorPolicy);
                //SetFeedback(1, GetLocalizedTextString("RecipeControllerRecipeLoaded"));
                //SetFeedback(FeedbackMessageType.UploadRecipeOK);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            //SetFeedback(2, e.Message);
            //SetFeedback(FeedbackMessageType.UploadRecipeError);
            return false;
        }
    }

    private bool SaveToDB(RecipeSchema curSchema, string RecipeName, CopyErrorPolicy ErrorPolicy)
    {


        RecipeSchema schema = curSchema;
        if (schema == null) return false;

        string name = RecipeName;
        if (String.IsNullOrEmpty(RecipeName))
        {
            return false;
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
                    //SetFeedback(FeedbackMessageType.SaveRecipeOK);
                    return true;
                }
                else
                {
                    // Create recipe
                    schema.CreateStoreRecipe(name);
                    // Save Recipe
                    schema.CopyToStoreRecipe(editModel.NodeId, name, ErrorPolicy);
                    //SetFeedback(1, $"{GetLocalizedTextString("RecipeControllerRecipe")} {name} {GetLocalizedTextString("RecipeControllerCreatedAndSaved")}");
                    //SetFeedback(FeedbackMessageType.NewRecipeOK);
                    return true ;
                }
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            //SetFeedback(2, e.Message);
            //SetFeedback(FeedbackMessageType.StoreRecipeError);
            return false;
        }
    }

    private bool SelectDataFromDatabase(RecipeSchema curSchema, string name)
    {
     
        if (curSchema == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }



        var editModelNode = curSchema.GetObject("EditModel");
        if (editModelNode == null)
        {
            return false;
        }
        CopyErrorPolicy ErrorPolicy = CopyErrorPolicy.StrictCopy;
        try
        {
            curSchema.CopyFromStoreRecipe(name, editModelNode.NodeId, ErrorPolicy);
         
            return true;
        }
        catch (Exception ex)
        {
            return false;

        }


    }


    private RecipeSchema GetRecipeShema(NodeId nodeId)
    {
        if (nodeId == null || nodeId == NodeId.Empty)
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
            }
            else
            {
                return null;
            }
        }
    }


    private Store GetSchemaStore(NodeId nodeId)
    {
        if (nodeId == null)
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

    private IUANode GetEditModel(RecipeSchema schema)
    {
        var editModel = schema.Get("EditModel");
        if (editModel == null)
        {
            //SetFeedback(2, $"{GetLocalizedTextString("RecipesEditorEditModelOfSchema")} {schema.BrowseName} {GetLocalizedTextString("RecipesEditorNotFound")}");

        }

        return editModel;

    }
}
