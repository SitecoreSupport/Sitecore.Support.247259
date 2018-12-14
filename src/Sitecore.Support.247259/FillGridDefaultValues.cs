namespace Sitecore.Support.XA.Foundation.Grid.Pipelines.ExecutePageEditorAction
{
  using Microsoft.Extensions.DependencyInjection;
  using Sitecore.Collections;
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.DependencyInjection;
  using Sitecore.Layouts;
  using Sitecore.Pipelines;
  using Sitecore.Pipelines.ExecutePageEditorAction;
  using Sitecore.Text;
  using Sitecore.XA.Foundation.Grid;
  using Sitecore.XA.Foundation.Multisite;
  using Sitecore.XA.Foundation.Presentation.Services;
  using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;
  using Sitecore.XA.Foundation.SitecoreExtensions.Repositories;
  using System.Collections.Generic;
  using System.Collections.Specialized;
  using System.Linq;

  public class FillGridDefaultValues
  {
    public IRenderingParametersService RenderingParametersService
    {
      get;
      set;
    }

    public FillGridDefaultValues()
    {
      RenderingParametersService = ServiceLocator.ServiceProvider.GetService<IRenderingParametersService>();
    }

    public void Process(PipelineArgs args)
    {
      IInsertRenderingArgs insertRenderingArgs = args as IInsertRenderingArgs;
      if (insertRenderingArgs != null && ServiceLocator.ServiceProvider.GetService<IMultisiteContext>().GetSiteItem(insertRenderingArgs.ContextItem) != null)
      {
        Item renderingItem = insertRenderingArgs.RenderingItem;
        string placeholderKey = insertRenderingArgs.PlaceholderKey;
        RenderingDefinition renderingDefinition = new RenderingDefinition
        {
          ItemID = renderingItem.ID.ToString(),
          Placeholder = placeholderKey
        };
        if (ContainsGridParametersField(renderingDefinition))
        {
          DeviceItem device = ServiceLocator.ServiceProvider.GetService<IContentRepository>().GetItem(insertRenderingArgs.Device.ID);
          Item gridDefinitionItem = ServiceLocator.ServiceProvider.GetService<IGridContext>().GetGridDefinitionItem(insertRenderingArgs.ContextItem, device);
          if (gridDefinitionItem != null)
          {
            string text = gridDefinitionItem[Sitecore.XA.Foundation.Grid.Templates.GridDefinition.Fields.DefaultGridParameters];
            text = text.TrimEnd('|');
            string text2 = text;
            string targetParameters = GetTargetParameters(insertRenderingArgs);
            text2 = (string.IsNullOrEmpty(targetParameters) ? text : targetParameters);
            NameValueCollection standardValues = RenderingParametersService.GetStandardValues(renderingDefinition);
            if (standardValues.AllKeys.Contains("GridParameters"))
            {
              standardValues["GridParameters"] = text2;
            }
            else
            {
              standardValues.Add("GridParameters", text2);
            }
            args.CustomData["RenderingParameters"] = new UrlString(standardValues).GetUrl();
          }
        }
      }
    }

    private List<RenderingDefinition> GetPlaceholderRenderings(DeviceDefinition definition, string placeholderKey)
    {
      List<RenderingDefinition> list = new List<RenderingDefinition>();
      foreach (RenderingDefinition rendering in definition.Renderings)
      {
        if (rendering.Placeholder.Equals(placeholderKey))
        {
          list.Add(rendering);
        }
      }
      return list;
    }

    protected virtual string GetTargetParameters(IInsertRenderingArgs args)
    {
      List<RenderingDefinition> placeholderRenderings = GetPlaceholderRenderings(args.Device, args.PlaceholderKey);
      if (placeholderRenderings.Count > 0 && args.Position < placeholderRenderings.Count)
      {
        return new UrlString(placeholderRenderings.ElementAt(args.Position).Parameters).Parameters["GridParameters"];
      }
      return string.Empty;
    }

    private bool ContainsGridParametersField(RenderingDefinition renderingDefinition)
    {
      FieldCollection fieldCollection = RenderingParametersService.GetStandardValuesItem(renderingDefinition)?.Fields;
      if (fieldCollection != null)
      {
        fieldCollection.ReadAll();
        if (fieldCollection.Any((Field field) => field.ID.Equals(Sitecore.XA.Foundation.Grid.Templates.GridParameters.Fields.GridParameters)))
        {
          return true;
        }
      }
      return false;
    }
  }
}