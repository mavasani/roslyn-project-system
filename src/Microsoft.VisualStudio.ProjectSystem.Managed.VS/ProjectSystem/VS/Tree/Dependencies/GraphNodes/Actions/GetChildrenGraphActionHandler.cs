﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions
{
    [Export(typeof(IDependenciesGraphActionHandler))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class GetChildrenGraphActionHandler : GraphActionHandlerBase
    {
        public const int Order = 110;

        [ImportingConstructor]
        public GetChildrenGraphActionHandler(IDependenciesGraphBuilder builder,
                                             IAggregateDependenciesSnapshotProvider aggregateSnpahostProvider)
            : base(builder, aggregateSnpahostProvider)
        {
        }

        public override bool CanHandleRequest(IGraphContext graphContext)
        {
            return graphContext.Direction == GraphContextDirection.Contains;
        }

        public override bool HandleRequest(IGraphContext graphContext)
        {
            var trackChanges = false;
            foreach (var inputGraphNode in graphContext.InputNodes)
            {
                if (graphContext.CancelToken.IsCancellationRequested)
                {
                    return trackChanges;
                }

                var projectPath = inputGraphNode.Id.GetValue(CodeGraphNodeIdName.Assembly);
                if (string.IsNullOrEmpty(projectPath))
                {
                    continue;
                }

                var dependency = GetDependency(graphContext, inputGraphNode);
                if (dependency == null)
                {
                    continue;
                }

                var viewProvider = ViewProviders.FirstOrDefault(x => x.Value.SupportsDependency(dependency));
                if (viewProvider == null)
                {
                    continue;
                }

                if (graphContext.TrackChanges)
                {
                    trackChanges = true;
                }

                using (var scope = new GraphTransactionScope())
                {
                    viewProvider.Value.BuildGraph(graphContext, projectPath, dependency, inputGraphNode);

                    scope.Complete();
                }
            }

            return trackChanges;
        }
    }
}
