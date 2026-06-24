// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

function getCart() {
    let cart = localStorage.getItem('ebay_cart');
    return cart ? JSON.parse(cart) : [];
}

function saveCart(cart) {
    localStorage.setItem('ebay_cart', JSON.stringify(cart));
    updateCartBadge();
}

function updateCartBadge() {
    let cart = getCart();
    let count = cart.reduce((sum, item) => sum + item.quantity, 0);
    // Assuming there is a badge with id 'cartBadge'
    let badge = document.getElementById('cartBadge');
    if(badge) {
        badge.innerText = count;
        badge.style.display = count > 0 ? 'inline-block' : 'none';
    }
}

function addToCart(productId, title, price, imageUrl) {
    let cart = getCart();
    let existing = cart.find(i => i.productId === productId);
    if(existing) {
        existing.quantity += 1;
    } else {
        cart.push({ productId, title, price, imageUrl, quantity: 1 });
    }
    saveCart(cart);
    alert('Added to cart!');
    syncCartServer();
}

function syncCartServer() {
    // If we have a JWT token in cookie, we sync it
    let tokenMatch = document.cookie.match(new RegExp('(^| )BearerToken=([^;]+)'));
    if (tokenMatch) {
         fetch('http://localhost:5001/api/Cart/sync', {
             method: 'POST',
             headers: {
                 'Content-Type': 'application/json',
                 'Authorization': 'Bearer ' + tokenMatch[2]
             },
             body: JSON.stringify({ items: getCart() })
         }).catch(console.error);
    }
}

// Call on load
document.addEventListener('DOMContentLoaded', () => {
    updateCartBadge();
});
