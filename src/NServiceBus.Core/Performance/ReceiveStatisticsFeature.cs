namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Features;

    class ReceiveStatisticsFeature : Feature
    {
        public ReceiveStatisticsFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            Console.Out.WriteLine("Core is setup");

            var logicalAddress = context.Settings.LogicalAddress();
            var performanceDiagnosticsBehavior = new ReceivePerformanceDiagnosticsBehavior(logicalAddress.EndpointInstance.Endpoint);

            context.RegisterStartupTask(new WarmupCooldownTask(performanceDiagnosticsBehavior));
            context.Pipeline.Register(performanceDiagnosticsBehavior, "Provides various performance counters for receive statistics");


            //todo: make this a separate feature
            context.Pipeline.Register("ProcessingStatistics", new ProcessingStatisticsBehavior(), "Collects timing for ProcessingStarted and adds the state to determine ProcessingEnded");
            context.Pipeline.Register("AuditProcessingStatistics", new AuditProcessingStatisticsBehavior(), "Add ProcessingStarted and ProcessingEnded headers");

        }

        class WarmupCooldownTask : FeatureStartupTask
        {
            public WarmupCooldownTask(ReceivePerformanceDiagnosticsBehavior behavior)
            {
                this.behavior = behavior;
            }

            protected override Task OnStart(IMessageSession session)
            {
                behavior.Warmup();
                return TaskEx.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session)
            {
                behavior.Cooldown();
                return TaskEx.CompletedTask;
            }

            readonly ReceivePerformanceDiagnosticsBehavior behavior;
        }
    }
}