load_assembly "System.Core"
load_assembly "System.Windows.Forms"
load_assembly "ChaKi.NET"
load_assembly "ChaKiEntity"
load_assembly "ChaKiService"

# KWIC Listのチェック済みレコードについて、
# 各レコードのcenter位置にSegmentを生成します。
# Segment Tagは次のtag変数にセットしてください。
tag = "NE"
current = ChaKi::MainForm.Instance.KwicView.GetModel()
records = current.KwicList.Records
records.each do |r|
  next if !r.Checked
  corpus = r.Crps
  svc = ChaKi::Service::DependencyEdit::DepEditService.new
  s = r.SenPos
  svc.Open(corpus, s, nil)
  d = r.Document.ID
  c = r.GetCenterCharOffset()
  svc.CenterWordStartAt = c
  begin
    c = r.GetCenterCharOffset()
    w = r.GetCenterCharLength()
    svc.SetupProject(0)
    svc.CreateSegment(c, c+w, tag)
    svc.Commit()
  rescue System::Exception => e
     printf("Error at: %d (%s)\n", s, e)
     next
  ensure
    svc.Close()
  end
  printf("OK: %d\n", s)
  r.Checked = false
end
ChaKi::MainForm.Instance.KwicView.Invoke(System::Action.new { ChaKi::MainForm.Instance.KwicView.Refresh() })
print "Done.\n"
