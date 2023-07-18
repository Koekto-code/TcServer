
function selectTimezone(val)
{
	const options = {
		method: 'POST'
	};

	fetch(`/manage/comp/settimezone?compname=${compName}&offset=${val}`, options)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
	});
}