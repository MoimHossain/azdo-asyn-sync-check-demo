﻿namespace PipelineChecks
{
    public class TaskProperties
    {
        public static TaskProperties GetTaskProperties(IHeaderDictionary requestHeaders)
        {
            IDictionary<string, string> taskProperties = new Dictionary<string, string>();

            foreach (var requestHeader in requestHeaders)
            {
                if (requestHeader.Value.Count != 0)
                {
                    var value = requestHeader.Value.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        taskProperties.Add(requestHeader.Key, value);
                    }
                }
            }

            return new TaskProperties(taskProperties);
        }
        public Guid ProjectId { get; }
        public string HubName { get; }
        public Guid PlanId { get; }
        public Uri PlanUri { get; }
        public Guid JobId { get; }
        public Guid TimelineId { get; }
        public Guid TaskInstanceId { get; set; }
        public string TaskInstanceName { get; }
        public string AuthToken { get; }
        public RequestType RequestType { get; }
        public IDictionary<string, string> MessageProperties { get; }

        private static readonly List<string> MandatoryProperties = new List<string>
        {
            ProjectIdKey,
            PlanIdKey,
            PlanUrlKey,
            JobIdKey,
            TimelineIdKey,
            AuthTokenKey,
        };

        public static readonly List<string> PropertiesList = new List<string>(MandatoryProperties)
        {
            HubNameKey,
            TaskInstanceIdKey,
            TaskInstanceNameKey,
            RequestTypeKey,
        };


        public TaskProperties(Guid projectId, string hubName, Guid planId, Uri planUri, Guid jobId, Guid timelineId, Guid taskInstanceId, string taskInstanceName, string authToken, RequestType requestType, IDictionary<string, string> messageProperties, List<string> validHubNameList)
        {
            ProjectId = projectId;
            HubName = hubName;
            PlanId = planId;
            PlanUri = planUri;
            JobId = jobId;
            TimelineId = timelineId;
            TaskInstanceId = taskInstanceId;
            TaskInstanceName = taskInstanceName;
            AuthToken = authToken;
            RequestType = requestType;
            MessageProperties = messageProperties;
            this.validHubNameList = validHubNameList;
        }

        public TaskProperties(IDictionary<string, string> rawProperties)
        {
            Dictionary<string, string> lowerCaseKeyMessageProperties = new Dictionary<string, string>();
            foreach (var messageProperty in rawProperties)
            {
                lowerCaseKeyMessageProperties.Add(messageProperty.Key.ToLower(), messageProperty.Value);
            }

            MessageProperties = lowerCaseKeyMessageProperties;

            var missingProperties = MandatoryProperties
                .Where(propertyToCheck => !lowerCaseKeyMessageProperties.ContainsKey(propertyToCheck)).ToList();
            if (missingProperties.Any())
            {
                var exceptionMessage =
                    $"Required properties '{string.Join(", ", missingProperties)}' are missing. Please provide these values and try again.";
                throw new InvalidDataException(exceptionMessage);
            }

            this.ProjectId = ParseGuid(ProjectIdKey);
            this.JobId = ParseGuid(JobIdKey);
            this.PlanId = ParseGuid(PlanIdKey);
            this.TimelineId = ParseGuid(TimelineIdKey);

            this.TaskInstanceId = lowerCaseKeyMessageProperties.ContainsKey(TaskInstanceIdKey)
                ? ParseGuid(TaskInstanceIdKey)
                : Guid.Empty;

            this.HubName = ParseMessageProperty(HubNameKey) ?? "Release";
            if (!validHubNameList.Contains(this.HubName, StringComparer.OrdinalIgnoreCase))
            {
                var exceptionMessage = $"Invalid hub name '{this.HubName}'. Please provide valid hub name from '{string.Join(", ", validHubNameList)}'.";
                throw new InvalidDataException(exceptionMessage);
            }

            var planUrl = ParseMessageProperty(PlanUrlKey);
            if (!Uri.TryCreate(planUrl, UriKind.Absolute, out var planUri))
            {
                var exceptionMessage = $"Invalid plan url '{planUrl}'. Please provide a valid url and try again.";
                throw new InvalidDataException(exceptionMessage);
            }
            this.PlanUri = planUri;

            this.AuthToken = ParseMessageProperty(AuthTokenKey);

            var requestTypeString = ParseMessageProperty(RequestTypeKey) ?? RequestType.Execute.ToString();
            if (Enum.TryParse<RequestType>(requestTypeString, out var requestType))
            {
                this.RequestType = requestType;
            }

            this.TaskInstanceName = ParseMessageProperty(TaskInstanceNameKey);
        }

        private string ParseMessageProperty(string messagePropertyKey)
        {
            if (MessageProperties.TryGetValue(messagePropertyKey, out var messagePropertyValue))
            {
                MessageProperties.Remove(messagePropertyKey);
            }
            return messagePropertyValue;
        }

        private Guid ParseGuid(string propertyName)
        {
            var messageProperty = MessageProperties[propertyName];
            if (!Guid.TryParse(messageProperty, out var projectId))
            {
                throw new InvalidDataException($"Invalid guid value '{messageProperty}' provided for {propertyName}");
            }

            MessageProperties.Remove(propertyName);

            return projectId;
        }

        private readonly List<string> validHubNameList = new List<string> { "build", "release", "gates", "checks" };
        private const string ProjectIdKey = "projectid";
        private const string JobIdKey = "jobid";
        private const string PlanIdKey = "planid";
        private const string TimelineIdKey = "timelineid";
        private const string TaskInstanceIdKey = "taskinstanceid";
        private const string HubNameKey = "hubname";
        private const string PlanUrlKey = "planurl";
        private const string AuthTokenKey = "authtoken";
        private const string RequestTypeKey = "requesttype";
        private const string TaskInstanceNameKey = "taskinstancename";
    }

    public enum RequestType
    {
        Execute,
        Cancel
    }
}
