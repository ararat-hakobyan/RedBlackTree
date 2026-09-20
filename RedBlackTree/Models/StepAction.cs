namespace RedBlackTree.Models;

public enum StepAction
{
    AddNode,

    BeforeRotateLeft,
    AfterRotateLeft,
    BeforeRotateRight,
    AfterRotateRight,

    ColorChange,

    BeginDelete,
    DeleteCaseLeftNil,
    DeleteCaseRightNil,
    FindSuccessor,
    SuccessorIsDirectChild,
    BeforeTransplantSuccessor,
    AfterTransplantSuccessor,
    BeforeReplaceWithSuccessor,
    AfterReplaceWithSuccessor,
    BeforeTransplant,
    AfterTransplant,
    BeginFixDelete,
    DeleteComplete,

    FixDeleteCase1,
    FixDeleteCase2,
    FixDeleteCase3,
    FixDeleteCase4,
    FixDeleteCase5,
    FixDeleteCase6,
    FixDeleteCase7,
    FixDeleteCase8,

    FindMinimumStart,
    FindMinimumStep,
    FindMaximumStart,
    FindMaximumStep,
    FindMaximumComplete,

    BeginSearch,
    SearchStep,
    SearchGoLeft,
    SearchGoRight,
    SearchFound,
    SearchNotFound
}
