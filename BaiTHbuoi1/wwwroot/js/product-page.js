// Mobile filter toggle
document.addEventListener('DOMContentLoaded', function () {
    // Show filter sidebar on mobile
    const showFilterBtn = document.getElementById('show-filter-btn');
    if (showFilterBtn) {
        showFilterBtn.addEventListener('click', function () {
            document.getElementById('filter-sidebar').classList.add('show');
            document.body.style.overflow = 'hidden'; // Prevent scrolling when filter is open
        });
    }

    // Hide filter sidebar on mobile
    const hideFilterBtn = document.getElementById('hide-filter-btn');
    if (hideFilterBtn) {
        hideFilterBtn.addEventListener('click', function () {
            document.getElementById('filter-sidebar').classList.remove('show');
            document.body.style.overflow = ''; // Restore scrolling
        });
    }

    // Set sort select value from URL
    const urlParams = new URLSearchParams(window.location.search);
    const sortValue = urlParams.get('sort');
    if (sortValue) {
        const sortSelect = document.getElementById('sort-select');
        if (sortSelect) {
            sortSelect.value = sortValue;
        }
    }

    // Handle status filter checkboxes
    const filterCheckboxes = document.querySelectorAll('.filter-checkbox');
    filterCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function () {
            applyFilters();
        });
    });
});

// Change sort function
function changeSort(value) {
    const url = new URL(window.location);
    url.searchParams.set('sort', value);
    window.location = url;
}

// Apply filters function
function applyFilters() {
    const url = new URL(window.location);

    // Get checkbox values
    const saleCheckbox = document.getElementById('sale-filter');
    const newCheckbox = document.getElementById('new-filter');

    if (saleCheckbox && saleCheckbox.checked) {
        url.searchParams.set('showSale', 'true');
    } else {
        url.searchParams.delete('showSale');
    }

    if (newCheckbox && newCheckbox.checked) {
        url.searchParams.set('showNew', 'true');
    } else {
        url.searchParams.delete('showNew');
    }

    window.location = url;
}

// Add to wishlist function
function addToWishlist(productId) {
    // You can implement AJAX call to add product to wishlist
    fetch('/Wishlist/Add', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ productId: productId })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Show success notification
                alert('Sản phẩm đã được thêm vào danh sách yêu thích!');
            } else {
                // Show error notification
                alert('Có lỗi xảy ra. Vui lòng thử lại sau!');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Có lỗi xảy ra. Vui lòng thử lại sau!');
        });
}

// Add to compare function
function addToCompare(productId) {
    // You can implement AJAX call to add product to compare list
    fetch('/Compare/Add', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ productId: productId })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Show success notification
                alert('Sản phẩm đã được thêm vào danh sách so sánh!');
            } else {
                // Show error notification
                alert('Có lỗi xảy ra. Vui lòng thử lại sau!');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Có lỗi xảy ra. Vui lòng thử lại sau!');
        });
}