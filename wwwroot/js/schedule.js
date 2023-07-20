
addContextMenu(document.getElementById('menu1'), document.getElementById('dots-btn1'));
addContextMenu(document.getElementById('excel-export-menu'), document.getElementById('excel-export-btn'));

function employeePage(innerid) {
	window.location.href = `/employee?compname=${compName}&id=${innerid}`;
}

function changeUnit(unitname) {
	window.location.href = `/schedule?compname=${compName}&unitname=${unitname}&viewdate=${viewDate}`;
}

function changeComp(compname) {
	window.location.href = `/schedule?compname=${compname}&viewdate=${viewDate}`;
}

function changeDate(newdate) {
	window.location.href = `/schedule?compname=${compName}&unitname=${unitName}&viewdate=${newdate}`;
}

function getExcelSheet() {
	window.location.href = `/schedule/xlsxreport?compname=${compName}&unitname=${unitName}&viewdate=${viewDate}`;
}

function delCookie(name) {
	document.cookie = name + '=; Path=/; Expires=Thu, 01 Jan 1970 00:00:01 GMT;'
}

function resetView()
{
	delCookie("scheduleCompName");
	delCookie("scheduleUnitName");
	delCookie("scheduleDate");
	window.location.href = '/schedule'
}

function toggleEmplSelection(val) {
	document.getElementsByName('empl-select').forEach(cb => {
		cb.checked = val;
	});
}

document.getElementById('empl-sel-title').addEventListener("input", (ev) => {
	ev.target.style.color = "black";
});

document.getElementById('empl-sel-name').addEventListener("input", (ev) => {
	ev.target.style.color = "black";
});

document.getElementById('dd2_h').addEventListener("click", (ev) => {
	ev.target.style.borderColor = "white";
});

function createEmployee()
{
	const empltitle = document.getElementById('empl-sel-title');
	const emplname = document.getElementById('empl-sel-name');
	const empladdr = document.getElementById('empl-sel-addr');
	const empldev = document.getElementById('dd2_h');
	
	if (!empltitle.value)
		empltitle.style.color = "red";

	if (!emplname.value)
		emplname.style.color = "red";

	const devnum = empldev.getAttribute("dd-value");
	console.log(devnum);

	if (!devnum)
		empldev.style.borderColor = "red";

	if (!empltitle.value || !emplname.value || !devnum)
		return;

	const options = {
		method: 'POST'
	};

	fetch(`/manage/employees/add?compname=${compName}&unitname=${unitName}&name=${emplname.value}&title=${empltitle.value}&addr=${empladdr.value}&dev=${devnum}`, options)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		window.location.reload();
	});
}

function deleteSelectedEmployees()
{
	const checkboxes = document.getElementsByName('empl-select');
	const values = []
	checkboxes.forEach(cb => {
		if (cb.checked)
			values.push(cb.value);
	});
	
	if (!window.confirm(`Удалить все данные выбранных сотрудников (${values.length})?`))
		return;

	const options = {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(values)
	};

	fetch(`/manage/employees/delete?compname=${compName}`, options)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		checkboxes.forEach(cb => {
			cb.checked = false;
		});
		window.location.reload();
	});
}

function transferEmployees(destination)
{
	const checkboxes = document.getElementsByName('empl-select');
	const values = []
	checkboxes.forEach(cb => {
		if (cb.checked)
			values.push(cb.value);
	});

	const options = {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(values)
	};

	fetch(`/manage/employees/transfer?compname=${compName}&destination=${destination}`, options)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		checkboxes.forEach(cb => {
			cb.checked = false;
		});
		window.location.reload();
	});
}

function createUnit(name)
{
	const options = {
		method: 'POST'
	};

	fetch(`/manage/unit/add?compname=${compName}&name=${name}`, options)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		window.location.reload();
	});
}

function deleteCurrentComp()
{
	if (!window.confirm("Удалить текущую организацию?"))
		return;
	
	const options = {
		method: 'POST'
	};

	fetch(`/manage/comp/delete?compname=${compName}`, options)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		window.location.href = "/schedule";
	});
}

function deleteCurrentUnit()
{
	if (!window.confirm("Удалить текущий отдел?"))
		return;
	
	const options = {
		method: 'POST'
	};

	fetch(`/manage/unit/delete?compname=${compName}&unitname=${unitName}`, options)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		changeComp(compName);
	});
}

function renameUnit(newname)
{
	const options = {
		method: 'POST'
	};

	fetch(`/manage/unit/rename?compname=${compName}&unitname=${unitName}&newname=${newname}`, options)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		changeUnit(newname);
	});
}

function exportCustomExcel()
{
	const ddex1 = document.getElementById('ddex1');
	const ddex2 = document.getElementById('ddex2');
	const ddex = document.getElementById('ddex_h');
	
	if (!ddex1.value)
		return;

	const ddexval = ddex.getAttribute("dd-value");

	let link = `/schedule/xlsxreport?compname=${compName}&viewdate=${ddex1.value}`;
	if (ddexval && (ddexval != "_all_"))
		link += `&unitname=${ddexval}`;
	if (ddex2.value)
		link += `&dateend=${ddex2.value}`;
	window.location.href = link;
}

// Company adding logic
{
	const pass = document.getElementById('comp-sel-pass');
	const pass1 = document.getElementById('comp-sel-pass1');
	const submit = document.getElementById('comp-submit-btn');
	const form = document.getElementById('comp-create-form');
	
	submit.style.pointerEvents = "none";
	const prevBgColor = submit.style.backgroundColor;
	submit.style.backgroundColor = "#aaa";
	
	const validate = (ev) => {
		if (pass.value == pass1.value) {
			submit.style.backgroundColor = prevBgColor;
			submit.style.pointerEvents = "all";
		} else {
			submit.style.backgroundColor = "#aaa";
			submit.style.pointerEvents = "none";
		}
	};
	
	pass.addEventListener('input', validate);
	pass1.addEventListener('input', validate);
	
	form.addEventListener('submit', (ev) => {
		ev.preventDefault();

		var formData = new FormData(form);

		var request = new Request('/register', {
			method: 'POST',
			headers: {
				'Content-Type': 'application/x-www-form-urlencoded'
			},
			body: new URLSearchParams(formData)
		});

		fetch(request)
		.then((response) => {
			if (!response.ok)
				throw new Error(`Fetch error: ${response.status}`);
			window.location.reload();
		})
	});
}

// Unit creating logic
{
	const textinp = document.getElementById('unit-create-ti');
	
	textinp.addEventListener('keypress', (ev) => {
		if (ev.key != 'Enter')
			return;
		createUnit(ev.target.value);
	});
}

// Unit renaming logic
{
	const textinp = document.getElementById('unit-rename-ti');
	
	textinp.addEventListener('keypress', (ev) => {
		if (ev.key != 'Enter')
			return;
		renameUnit(ev.target.value);
	});
}

// "Search by filter" logic
{
	const tbody = document.getElementById('tbody1');
	const trows = Array.from(tbody.querySelectorAll('tr'));
	const filter = document.getElementById('filter1');

	filter.addEventListener('input', () => {
		const filteredRows = trows.filter(row => {
			if (!filter.value)
				return true;
			const fst = row.querySelector("td:first-child").innerText.toLowerCase();
			const snd = row.querySelector("td:nth-child(2)").innerText.toLowerCase();
			const trd = row.querySelector("td:nth-child(3)").innerText.toLowerCase();
			if (! (fst || snd || trd)) return true;
			let ok = false;
			
			const fval = filter.value.toLowerCase();
			if (fst) ok |= fst.includes(fval);
			if (snd) ok |= snd.includes(fval);
			if (trd) ok |= trd.includes(fval);
			return ok;
		});
		tbody.innerHTML = "";
		filteredRows.forEach(row => {
			tbody.appendChild(row);
		})
	});
}