function applyNumberMask(event) {
    const input = event.target;
    input.value = input.value.replace(/\D/g, ''); // Remove non-numeric characters
}
