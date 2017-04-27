﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.MakeFieldReadonly

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.MakeFieldReadonly
    Public Class MakeFieldReadonlyTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicMakeFieldReadonlyDiagnosticAnalyzer(),
                New VisualBasicMakeFieldReadonlyCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldIsReadonly() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private ReadOnly [|_foo|] As Integer
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldNotAssigned() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer
End Class",
"Class C
    Private ReadOnly _foo As Integer
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldNotAssigned_Struct() As Task
            Await TestInRegularAndScriptAsync(
"Structure C
    Private [|_foo|] As Integer
End Structure",
"Structure C
    Private ReadOnly _foo As Integer
End Structure")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldNotAssigned_Module() As Task
            Await TestInRegularAndScriptAsync(
"Module C
    Private [|_foo|] As Integer
End Module",
"Module C
    Private ReadOnly _foo As Integer
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldNotAssigned_FieldDeclaredWithDim() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim [|_foo|] As Integer
End Class",
"Class C
    ReadOnly _foo As Integer
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInline() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
End Class",
"Class C
    Private ReadOnly _foo As Integer = 0
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_AllCanBeReadonly() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0, _bar As Integer = 0
End Class",
"Class C
    Private _bar As Integer = 0
    Private ReadOnly _foo As Integer = 0
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_OneAssignedInMethod() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private _foo As Integer = 0, [|_bar|] As Integer = 0
    Private Sub Foo()
        _foo = 0
    End Sub
End Class",
"Class C
    Private _foo As Integer = 0
    Private ReadOnly _bar As Integer = 0
    Private Sub Foo()
        _foo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInCtor() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    Public Sub New()
        _foo = 0
    End Sub
End Class",
"Class C
    Private ReadOnly _foo As Integer = 0
    Public Sub New()
        _foo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldReturnedInProperty() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    ReadOnly Property Foo As Integer
        Get
            Return _foo
        End Get
    End Property
End Class",
"Class C
    Private ReadOnly _foo As Integer = 0
    ReadOnly Property Foo As Integer
        Get
            Return _foo
        End Get
    End Property
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInProperty() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    ReadOnly Property Foo As Integer
        Get
            Return _foo
        End Get
        Set(value As Integer)
            _foo = value
        End Set
    End Property
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    Sub Foo
        _foo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInMethodWithCompoundOperator() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    Sub Foo
        _foo += 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function AssignedInPartialClass() As Task
            Await TestMissingInRegularAndScriptAsync(
"Partial Class C
    Private [|_foo|] As Integer = 0
End Class

Partial Class C
    Sub Foo()
        _foo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function PassedAsByRefParameter() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    Sub Foo()
        Bar(_foo)
    End Sub
    Sub Bar(ByRef value As Integer)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function PassedAsByValParameter() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    Sub Foo()
        Bar(_foo)
    End Sub
    Sub Bar(ByVal value As Integer)
    End Sub
End Class",
"Class C
    Private ReadOnly _foo As Integer = 0
    Sub Foo()
        Bar(_foo)
    End Sub
    Sub Bar(ByVal value As Integer)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function SharedFieldAssignedInSharedCtor() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private Shared [|_foo|] As Integer = 0
    Shared Sub New()
        _foo = 0
    End Sub
End Class",
"Class C
    Private Shared ReadOnly _foo As Integer = 0
    Shared Sub New()
        _foo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function SharedFieldAssignedInNonSharedCtor() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private Shared [|_foo|] As Integer = 0
    Sub New()
        _foo = 0
    End Sub
End Class")
        End Function
    End Class
End Namespace