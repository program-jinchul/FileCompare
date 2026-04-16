using System;
using System.Data;
using System.Drawing;      // Color 구조체를 쓰기 위해 필수!
using System.IO;           // Directory, FileInfo를 쓰기 위해 필수!
using System.Linq;         // Select, OrderBy를 쓰기 위해 필수!
using System.Windows.Forms;

namespace FileCompare
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void PopulateListView(ListView lv, string folderPath)
        {
            lv.BeginUpdate(); lv.Items.Clear();
            try
            { // 폴더(디렉터리) 먼저 추가
                var dirs = Directory.EnumerateDirectories(folderPath)
                    .Select(p => new DirectoryInfo(p)).OrderBy(d => d.Name);
                foreach (var d in dirs)
                {
                    var item = new ListViewItem(d.Name);
                    item.SubItems.Add("<DIR>");
                    item.SubItems.Add(d.LastWriteTime.ToString("g"));
                    lv.Items.Add(item);
                }
                // 파일 추가
                var files = Directory.EnumerateFiles(folderPath)
                    .Select(p => new FileInfo(p)).OrderBy(f => f.Name);
                foreach (var f in files)
                {
                    var item = new ListViewItem(f.Name);
                    item.SubItems.Add(f.Length.ToString("N0") + " 바이트");
                    item.SubItems.Add(f.LastWriteTime.ToString("g"));
                    lv.Items.Add(item);
                }
                // 컬럼 너비 자동 조정(컨텐츠 기준)
                for (int i = 0; i < lv.Columns.Count; i++)
                {
                    lv.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent);
                }
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show(this, "폴더를 찾을 수 없습니다.", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show(this, "입출력 오류: " + ex.Message, "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                lv.EndUpdate();
            }
        }

        private void CompareFiles()
        {
            // 양쪽 리스트뷰에 아이템이 없으면 중단
            if (lvwLeftDir.Items.Count == 0 && lvwRightDir.Items.Count == 0) return;

            // 모든 아이템 일단 검은색으로 초기화
            foreach (ListViewItem item in lvwLeftDir.Items) item.ForeColor = Color.Black;
            foreach (ListViewItem item in lvwRightDir.Items) item.ForeColor = Color.Black;
            // 왼쪽 기준 비교
            foreach (ListViewItem leftItem in lvwLeftDir.Items)
            {
                // 오른쪽 리스트에서 같은 이름 찾기
                ListViewItem rightItem = FindItemByName(lvwRightDir, leftItem.Text);

                if (rightItem == null) // 왼쪽만 있음
                {
                    leftItem.ForeColor = Color.Purple;
                }
                else // 양쪽 다 있음 -> 시간 비교
                {
                    DateTime leftTime = DateTime.Parse(leftItem.SubItems[2].Text);
                    DateTime rightTime = DateTime.Parse(rightItem.SubItems[2].Text);

                    if (leftTime > rightTime) { leftItem.ForeColor = Color.Red; rightItem.ForeColor = Color.Gray; }
                    else if (leftTime < rightTime) { leftItem.ForeColor = Color.Gray; rightItem.ForeColor = Color.Red; }
                }
            }

            // 오른쪽 단독 파일 처리 (오른쪽에만 있는 보라색 찾기)
            foreach (ListViewItem rightItem in lvwRightDir.Items)
            {
                if (FindItemByName(lvwLeftDir, rightItem.Text) == null)
                {
                    rightItem.ForeColor = Color.Purple;
                }
            }
        }

        // 이름으로 아이템 찾아주는 도우미 함수
        private ListViewItem FindItemByName(ListView lv, string name)
        {
            foreach (ListViewItem item in lv.Items)
            {
                if (item.Text == name) return item;
            }
            return null;
        }
        private void button4_Click(object sender, EventArgs e)
        {

        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnLeftDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "폴더를 선택하세요.";
                // 현재 텍스트박스에 있는 경로를 초기 선택 폴더로 설정
                if (!string.IsNullOrWhiteSpace(txtLeftDir.Text) &&
                    Directory.Exists(txtLeftDir.Text))
                {
                    dlg.SelectedPath = txtLeftDir.Text;
                }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtLeftDir.Text = dlg.SelectedPath;
                }
            }
        }

        private void btnRightDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "폴더를 선택하세요.";
                // 현재 텍스트박스에 있는 경로를 초기 선택 폴더로 설정
                if (!string.IsNullOrWhiteSpace(txtRightDir.Text) &&
                    Directory.Exists(txtRightDir.Text))
                {
                    dlg.SelectedPath = txtRightDir.Text;
                }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtRightDir.Text = dlg.SelectedPath;
                }
            }
        }

        private void txtLeftDir_TextChanged(object sender, EventArgs e)
        {
            if (Directory.Exists(txtLeftDir.Text))
            {
                PopulateListView(lvwLeftDir, txtLeftDir.Text);
                CompareFiles(); // 색상 비교 함수 호출
            }
        }

        private void txtRightDir_TextChanged(object sender, EventArgs e)
        {
            if (Directory.Exists(txtRightDir.Text))
            {
                PopulateListView(lvwRightDir, txtRightDir.Text);
                CompareFiles(); // 색상 비교 함수 호출
            }
        }

        private void btnCopyFromLeft_Click(object sender, EventArgs e)
        {
            // 1. 왼쪽 리스트뷰(lvwLeftDir)에서 선택된 항목이 있는지 확인
            if (lvwLeftDir.SelectedItems.Count == 0)
            {
                MessageBox.Show("복사할 파일을 왼쪽 목록에서 선택해주세요.");
                return;
            }

            // 2. 경로 설정
            string sourceDir = txtLeftDir.Text;
            string targetDir = txtRightDir.Text;

            // 3. 선택된 항목들을 루프 돌며 복사
            foreach (ListViewItem item in lvwLeftDir.SelectedItems)
            {
                // 폴더(<DIR>)인 경우는 제외하고 파일만 처리
                if (item.SubItems[1].Text == "<DIR>") continue;

                string fileName = item.Text;
                string sourcePath = Path.Combine(sourceDir, fileName);
                string targetPath = Path.Combine(targetDir, fileName);

                try
                {
                    // 파일 복사 (true: 이미 파일이 존재하면 최신 내용으로 덮어쓰기)
                    File.Copy(sourcePath, targetPath, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{fileName} 복사 중 오류: {ex.Message}");
                }
            }

            // 4. 복사 완료 후 리스트 갱신 및 색상 다시 비교 (중요!)
            PopulateListView(lvwLeftDir, sourceDir);
            PopulateListView(lvwRightDir, targetDir);
            CompareFiles();

            MessageBox.Show("왼쪽에서 오른쪽으로 복사가 완료되었습니다!");
        }
    }
}
    
