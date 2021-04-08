module ProjectSync.Tests.Reporters

open ApprovalTests.Reporters;

#if SILENT

[<assembly: UseReporter(typeof<QuietReporter>)>]

#else

[<assembly: UseReporter(typeof<DiffReporter>)>]

#endif

do()