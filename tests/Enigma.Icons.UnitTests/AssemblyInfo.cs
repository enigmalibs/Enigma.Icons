using Xunit;

// SvgIconParser.MaxDocumentBytes is process-global mutable state (SPEC §5). A test that lowers it
// would otherwise be observable by tests running concurrently in other classes, so the whole
// assembly runs serially. Do not re-enable parallelization without reworking those tests.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
