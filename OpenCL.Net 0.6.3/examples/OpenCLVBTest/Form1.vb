Imports System.IO
Imports System.Drawing.Imaging
Imports OpenCLNet


Public Class Form1
    Dim platform As OpenCLNet.Platform
    Dim context As OpenCLNet.Context
    Dim program As OpenCLNet.Program
    Dim kernel As OpenCLNet.Kernel
    Dim bitmap As System.Drawing.Bitmap
    Dim cq As OpenCLNet.CommandQueue
    Dim devices
    Dim mandelbrotMemBuffer As OpenCLNet.Mem

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Try
            platform = OpenCLNet.OpenCL.GetPlatform(0)
            devices = platform.QueryDevices(DeviceType.ALL)
            context = platform.CreateDefaultContext(Nothing, Nothing)
            cq = context.CreateCommandQueue(devices(0), 0)
            program = context.CreateProgramWithSource(System.IO.File.ReadAllText("Mandelbrot.cl"))
            program.Build()
        Catch ex As Exception
            Print(ex.ToString())
        End Try

        kernel = program.CreateKernel("Mandelbrot")
        bitmap = New Bitmap(1024, 1024, System.Drawing.Imaging.PixelFormat.Format32bppArgb)
        mandelbrotMemBuffer = context.CreateBuffer(MemFlags.WRITE_ONLY, bitmap.Width * bitmap.Height * 4, Nothing)
    End Sub

    Private Sub DrawMandelbrot(ByVal g As Graphics)
        CalcMandelbrot()
        g.DrawImageUnscaled(bitmap, 0, 0)
    End Sub

    Private Sub CalcMandelbrot()
        Dim bd = bitmap.LockBits(New Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb)
        Dim clEvent As OpenCLNet.Event
        Dim globalWorkSize(0 To 1) As IntPtr
        Dim eventWaitList(0 To 0) As OpenCLNet.Event
        Dim _Left
        Dim _Top
        Dim _Right
        Dim _Bottom
        Dim offset As Integer
        Dim cb As Integer
        Dim ptr As System.IntPtr

        _Left = -2.0
        _Top = 2.0
        _Right = 2.0
        _Bottom = -2.0
        kernel.SetSingleArg(0, _Left)
        kernel.SetSingleArg(1, _Top)
        kernel.SetSingleArg(2, _Right)
        kernel.SetSingleArg(3, _Bottom)
        kernel.SetIntArg(4, bitmap.Width)
        kernel.SetMemArg(5, mandelbrotMemBuffer)

        clEvent = Nothing
        globalWorkSize(0) = New IntPtr(CType(bd.Width, Long))
        globalWorkSize(1) = New IntPtr(CType(bd.Height, Long))
        cq.EnqueueNDRangeKernel(kernel, 2, Nothing, globalWorkSize, Nothing, 0, Nothing, clEvent)
        cq.Finish()
        For i = 0 To bitmap.Width - 1
            offset = bitmap.Width * 4 * i
            cb = bitmap.Width * 4
            ptr = bd.Scan0.ToInt32 + bd.Stride * i
            cq.EnqueueReadBuffer(mandelbrotMemBuffer, False, offset, cb, ptr)
        Next i
        cq.Finish()
        bitmap.UnlockBits(bd)
    End Sub

    Private Sub Form1_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles MyBase.Paint
        Try
            DrawMandelbrot(e.Graphics)

        Catch ex As Exception
            Print(ex.ToString())
        End Try
    End Sub
End Class
