/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class MeshArrayPropertyInLoopTests : BaseCodeFixVerifierTest<MeshArrayPropertyInLoopAnalyzer, MeshArrayPropertyInLoopCodeFix>
{
	[Fact]
	public async Task VerticesInForLoopCondition()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            // some work
        }
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(9, 29)
			.WithArguments("vertices");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] meshVertices = mesh.vertices;
        for (int i = 0; i < meshVertices.Length; i++)
        {
            // some work
        }
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task VerticesInForLoopBody()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        for (int i = 0; i < 10; i++)
        {
            var v = mesh.vertices[i];
        }
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(11, 21)
			.WithArguments("vertices");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] meshVertices = mesh.vertices;
        for (int i = 0; i < 10; i++)
        {
            var v = meshVertices[i];
        }
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task NormalsInWhileLoop()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        int i = 0;
        while (i < mesh.normals.Length)
        {
            i++;
        }
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(10, 20)
			.WithArguments("normals");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        int i = 0;
        Vector3[] meshNormals = mesh.normals;
        while (i < meshNormals.Length)
        {
            i++;
        }
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task ColorsInForEachLoop()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        foreach (var c in mesh.colors)
        {
            // some work
        }
    }
}
";

		// Note: foreach evaluates the collection expression only once at the start,
		// so this is actually not inside the loop body that repeats.
		// We should NOT report this case.
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task VerticesOutsideLoop()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        var vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            // some work
        }
    }
}
";

		// No diagnostic - vertices is accessed outside the loop
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task MultiplePropertiesInLoop()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            mesh.vertices[i] += mesh.normals[i] * Time.deltaTime;
        }
    }
}
";

		var diagnostic1 = ExpectDiagnostic()
			.WithLocation(11, 13)
			.WithArguments("vertices");

		var diagnostic2 = ExpectDiagnostic()
			.WithLocation(11, 33)
			.WithArguments("normals");

		await VerifyCSharpDiagnosticAsync(test, diagnostic1, diagnostic2);
	}

	[Fact]
	public async Task UvPropertyInLoop()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        for (int i = 0; i < mesh.uv.Length; i++)
        {
            // some work
        }
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(9, 29)
			.WithArguments("uv");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector2[] meshUv = mesh.uv;
        for (int i = 0; i < meshUv.Length; i++)
        {
            // some work
        }
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TrianglesPropertyNotReported()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        for (int i = 0; i < mesh.triangles.Length; i++)
        {
            // some work
        }
    }
}
";

		// triangles returns int[] which is not in our list of allocating array element types
		// (we only track Vector2[], Vector3[], Vector4[], Color[], Color32[])
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task TangentsPropertyInLoop()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        for (int i = 0; i < mesh.tangents.Length; i++)
        {
            // some work
        }
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(9, 29)
			.WithArguments("tangents");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector4[] meshTangents = mesh.tangents;
        for (int i = 0; i < meshTangents.Length; i++)
        {
            // some work
        }
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task DoWhileLoop()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        int i = 0;
        do
        {
            var v = mesh.vertices[i];
            i++;
        } while (i < 10);
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(12, 21)
			.WithArguments("vertices");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        int i = 0;
        Vector3[] meshVertices = mesh.vertices;
        do
        {
            var v = meshVertices[i];
            i++;
        } while (i < 10);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task DoWhileLoopCondition()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        int i = 0;
        do
        {
            i++;
        } while (i < mesh.vertices.Length);
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(13, 22)
			.WithArguments("vertices");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        int i = 0;
        Vector3[] meshVertices = mesh.vertices;
        do
        {
            i++;
        } while (i < meshVertices.Length);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task NestedLoop()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < mesh.vertices.Length; j++)
            {
                // some work
            }
        }
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(11, 33)
			.WithArguments("vertices");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        for (int i = 0; i < 10; i++)
        {
            Vector3[] meshVertices = mesh.vertices;
            for (int j = 0; j < meshVertices.Length; j++)
            {
                // some work
            }
        }
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task VertexCountNotReported()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            // some work - using vertexCount is fine, no allocation
        }
    }
}
";

		// vertexCount is not an allocating property
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task LocalFunctionDoesNotCrossLoopBoundary()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        for (int i = 0; i < 10; i++)
        {
            LocalFunction();
        }

        void LocalFunction()
        {
            // This is not inside the loop body from analyzer's perspective
            var v = mesh.vertices;
        }
    }
}
";

		// The local function is not considered inside the loop
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task LambdaDoesNotCrossLoopBoundary()
	{
		const string test = @"
using UnityEngine;
using System;

class Camera : MonoBehaviour
{
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        for (int i = 0; i < 10; i++)
        {
            Action a = () => {
                // This is not inside the loop body from analyzer's perspective
                var v = mesh.vertices;
            };
        }
    }
}
";

		// The lambda is not considered inside the loop
		await VerifyCSharpDiagnosticAsync(test);
	}
}
