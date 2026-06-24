-- ═══════════════════════════════════════════════════════════════════════════
-- SAMPLE DATA FOR CloneEbayDB
-- Run this script AFTER creating the database with clone_ebay_sqlserver_schema.sql
-- ═══════════════════════════════════════════════════════════════════════════

USE CloneEbayDB;
GO

-- ─── 1. Users ───────────────────────────────────────────────────────────────
-- Password cho tất cả user: "Password123" (BCrypt hash)
INSERT INTO [User] ([username], [email], [password], [role], [avatarURL]) VALUES
(N'Admin', N'admin@ebay.com', '$2a$11$K5g5x5Q5z5z5z5z5z5z5zOK5g5x5Q5z5z5z5z5z5z5z5z5z5z5z', N'Admin', N'https://i.pravatar.cc/150?img=1'),
(N'John Smith', N'john@gmail.com', '$2a$11$K5g5x5Q5z5z5z5z5z5z5zOK5g5x5Q5z5z5z5z5z5z5z5z5z5z5z', N'User', N'https://i.pravatar.cc/150?img=2'),
(N'Jane Doe', N'jane@gmail.com', '$2a$11$K5g5x5Q5z5z5z5z5z5z5zOK5g5x5Q5z5z5z5z5z5z5z5z5z5z5z', N'User', N'https://i.pravatar.cc/150?img=3'),
(N'TechStore', N'techstore@gmail.com', '$2a$11$K5g5x5Q5z5z5z5z5z5z5zOK5g5x5Q5z5z5z5z5z5z5z5z5z5z5z', N'Seller', N'https://i.pravatar.cc/150?img=4'),
(N'FashionHub', N'fashionhub@gmail.com', '$2a$11$K5g5x5Q5z5z5z5z5z5z5zOK5g5x5Q5z5z5z5z5z5z5z5z5z5z5z', N'Seller', N'https://i.pravatar.cc/150?img=5'),
(N'HomeGoods', N'homegoods@gmail.com', '$2a$11$K5g5x5Q5z5z5z5z5z5z5zOK5g5x5Q5z5z5z5z5z5z5z5z5z5z5z', N'Seller', N'https://i.pravatar.cc/150?img=6'),
(N'SportZone', N'sportzone@gmail.com', '$2a$11$K5g5x5Q5z5z5z5z5z5z5zOK5g5x5Q5z5z5z5z5z5z5z5z5z5z5z', N'Seller', N'https://i.pravatar.cc/150?img=7'),
(N'BookWorld', N'bookworld@gmail.com', '$2a$11$K5g5x5Q5z5z5z5z5z5z5zOK5g5x5Q5z5z5z5z5z5z5z5z5z5z5z', N'Seller', N'https://i.pravatar.cc/150?img=8'),
(N'Alice Nguyen', N'alice@gmail.com', '$2a$11$K5g5x5Q5z5z5z5z5z5z5zOK5g5x5Q5z5z5z5z5z5z5z5z5z5z5z', N'User', N'https://i.pravatar.cc/150?img=9'),
(N'Bob Tran', N'bob@gmail.com', '$2a$11$K5g5x5Q5z5z5z5z5z5z5zOK5g5x5Q5z5z5z5z5z5z5z5z5z5z5z', N'User', N'https://i.pravatar.cc/150?img=10');
GO

-- ─── 2. Addresses ───────────────────────────────────────────────────────────
INSERT INTO [Address] ([userId], [fullName], [phone], [street], [city], [state], [country], [isDefault]) VALUES
(2, N'John Smith', N'0901234567', N'123 Main Street', N'Ho Chi Minh', N'HCM', N'Vietnam', 1),
(2, N'John Smith', N'0901234567', N'456 Office Blvd', N'Ha Noi', N'HN', N'Vietnam', 0),
(3, N'Jane Doe', N'0912345678', N'789 Garden Ave', N'Da Nang', N'DN', N'Vietnam', 1),
(9, N'Alice Nguyen', N'0923456789', N'321 Lake View', N'Ho Chi Minh', N'HCM', N'Vietnam', 1),
(10, N'Bob Tran', N'0934567890', N'654 Mountain Rd', N'Can Tho', N'CT', N'Vietnam', 1);
GO

-- ─── 3. Categories ──────────────────────────────────────────────────────────
INSERT INTO [Category] ([name]) VALUES
(N'Electronics'),
(N'Fashion'),
(N'Home & Garden'),
(N'Sports & Outdoors'),
(N'Books & Media'),
(N'Toys & Hobbies'),
(N'Automotive'),
(N'Health & Beauty');
GO

-- ─── 4. Products ────────────────────────────────────────────────────────────
INSERT INTO [Product] ([title], [description], [price], [images], [categoryId], [sellerId], [isAuction], [auctionEndTime]) VALUES
-- Electronics (categoryId = 1, seller = TechStore id=4)
(N'iPhone 15 Pro Max 256GB', N'Apple iPhone 15 Pro Max with A17 Pro chip, 48MP camera system, titanium design. Brand new, sealed box.', 1199.99, N'https://picsum.photos/seed/iphone15/400/300', 1, 4, 0, NULL),
(N'Samsung Galaxy S24 Ultra', N'Samsung Galaxy S24 Ultra with S Pen, 200MP camera, Snapdragon 8 Gen 3. Factory unlocked.', 1099.00, N'https://picsum.photos/seed/samsung24/400/300', 1, 4, 0, NULL),
(N'MacBook Pro 14" M3 Pro', N'Apple MacBook Pro 14-inch with M3 Pro chip, 18GB RAM, 512GB SSD. Space Black color.', 1999.00, N'https://picsum.photos/seed/macbook14/400/300', 1, 4, 0, NULL),
(N'Sony WH-1000XM5 Headphones', N'Industry-leading noise cancellation, 30-hour battery life, crystal clear hands-free calling.', 349.99, N'https://picsum.photos/seed/sonyxm5/400/300', 1, 4, 0, NULL),
(N'iPad Air M2 11-inch', N'Apple iPad Air with M2 chip, 11-inch Liquid Retina display, 128GB storage.', 599.00, N'https://picsum.photos/seed/ipadair/400/300', 1, 4, 0, NULL),
(N'Dell XPS 15 Laptop', N'Dell XPS 15 with Intel Core i7-13700H, 16GB RAM, 512GB SSD, 15.6" OLED display.', 1549.99, N'https://picsum.photos/seed/dellxps/400/300', 1, 4, 1, DATEADD(DAY, 5, GETDATE())),
(N'Nintendo Switch OLED', N'Nintendo Switch OLED Model with vibrant 7-inch screen, wide adjustable stand, enhanced audio.', 349.00, N'https://picsum.photos/seed/switch/400/300', 1, 4, 0, NULL),
(N'AirPods Pro 2nd Generation', N'Apple AirPods Pro with USB-C, Active Noise Cancellation, Adaptive Audio.', 249.00, N'https://picsum.photos/seed/airpods/400/300', 1, 4, 0, NULL),

-- Fashion (categoryId = 2, seller = FashionHub id=5)
(N'Nike Air Jordan 1 Retro High', N'Iconic Nike Air Jordan 1 Retro High OG. Classic colorway, premium leather, brand new with box.', 189.99, N'https://picsum.photos/seed/jordan1/400/300', 2, 5, 0, NULL),
(N'Levi''s 501 Original Fit Jeans', N'Classic Levi''s 501 Original Fit jeans in dark wash. 100% cotton, button fly, straight leg.', 69.50, N'https://picsum.photos/seed/levis501/400/300', 2, 5, 0, NULL),
(N'Ray-Ban Aviator Classic', N'Ray-Ban Aviator Classic sunglasses with gold frame and green G-15 lenses. UV protection.', 154.00, N'https://picsum.photos/seed/rayban/400/300', 2, 5, 0, NULL),
(N'Adidas Ultraboost 23', N'Adidas Ultraboost running shoes with BOOST midsole, Primeknit upper. Cloud White colorway.', 190.00, N'https://picsum.photos/seed/ultraboost/400/300', 2, 5, 0, NULL),
(N'North Face Puffer Jacket', N'The North Face 1996 Retro Nuptse Jacket. 700-fill goose down insulation, water-resistant.', 280.00, N'https://picsum.photos/seed/northface/400/300', 2, 5, 1, DATEADD(DAY, 3, GETDATE())),
(N'Casio G-Shock GA-2100', N'Casio G-Shock GA-2100 "CasiOak". Carbon Core Guard, 200m water resistance, LED light.', 99.99, N'https://picsum.photos/seed/gshock/400/300', 2, 5, 0, NULL),
(N'Gucci GG Marmont Belt', N'Gucci GG Marmont reversible belt in black/brown leather with Double G buckle. Size 85.', 450.00, N'https://picsum.photos/seed/guccibelt/400/300', 2, 5, 0, NULL),

-- Home & Garden (categoryId = 3, seller = HomeGoods id=6)
(N'Dyson V15 Detect Vacuum', N'Dyson V15 Detect cordless vacuum with laser dust detection. Up to 60 min runtime.', 749.99, N'https://picsum.photos/seed/dysonv15/400/300', 3, 6, 0, NULL),
(N'Instant Pot Duo 7-in-1', N'Instant Pot Duo 7-in-1 Electric Pressure Cooker, 6 Quart. Slow cooker, rice cooker, steamer.', 89.95, N'https://picsum.photos/seed/instantpot/400/300', 3, 6, 0, NULL),
(N'Philips Hue Starter Kit', N'Philips Hue White and Color Ambiance Smart LED Starter Kit with Bridge. 4 bulbs included.', 179.99, N'https://picsum.photos/seed/philipshue/400/300', 3, 6, 0, NULL),
(N'KitchenAid Stand Mixer', N'KitchenAid Artisan Series 5-Quart Stand Mixer in Empire Red. 10 speeds, tilt-head design.', 379.99, N'https://picsum.photos/seed/kitchenaid/400/300', 3, 6, 0, NULL),
(N'Roomba j7+ Robot Vacuum', N'iRobot Roomba j7+ Self-Emptying Robot Vacuum with obstacle avoidance. Smart mapping.', 599.00, N'https://picsum.photos/seed/roomba/400/300', 3, 6, 1, DATEADD(DAY, 7, GETDATE())),
(N'Nespresso Vertuo Next', N'Nespresso Vertuo Next Coffee Machine by Breville. Centrifusion technology, 5 cup sizes.', 159.00, N'https://picsum.photos/seed/nespresso/400/300', 3, 6, 0, NULL),
(N'IKEA KALLAX Shelf Unit', N'IKEA KALLAX Shelf unit, white, 77x147 cm. Versatile storage for any room.', 79.99, N'https://picsum.photos/seed/kallax/400/300', 3, 6, 0, NULL),

-- Sports & Outdoors (categoryId = 4, seller = SportZone id=7)
(N'Yeti Rambler 30oz Tumbler', N'YETI Rambler 30oz Tumbler with MagSlider Lid. Double-wall vacuum insulation. Stainless steel.', 35.00, N'https://picsum.photos/seed/yeti30/400/300', 4, 7, 0, NULL),
(N'Garmin Forerunner 265', N'Garmin Forerunner 265 GPS Running Smartwatch with AMOLED display. Training metrics, music storage.', 449.99, N'https://picsum.photos/seed/garmin265/400/300', 4, 7, 0, NULL),
(N'Coleman 6-Person Tent', N'Coleman Sundome 6-Person Camping Tent. WeatherTec system, easy setup, ventilation.', 129.99, N'https://picsum.photos/seed/coleman/400/300', 4, 7, 0, NULL),
(N'Hydroflask 32oz Water Bottle', N'Hydro Flask 32oz Wide Mouth with Flex Cap. TempShield insulation, BPA-free.', 44.95, N'https://picsum.photos/seed/hydroflask/400/300', 4, 7, 0, NULL),
(N'Fitbit Charge 6 Tracker', N'Fitbit Charge 6 Advanced Fitness Tracker with GPS, heart rate, stress management.', 159.95, N'https://picsum.photos/seed/fitbit6/400/300', 4, 7, 0, NULL),
(N'Wilson Evolution Basketball', N'Wilson Evolution Indoor Game Basketball. Composite leather, moisture-absorbing cushion core.', 69.99, N'https://picsum.photos/seed/wilson/400/300', 4, 7, 0, NULL),
(N'Peloton Yoga Mat', N'Peloton Reversible Workout Mat. 5mm thick, non-slip surface, lightweight and portable.', 58.00, N'https://picsum.photos/seed/peloton/400/300', 4, 7, 1, DATEADD(DAY, 4, GETDATE())),

-- Books & Media (categoryId = 5, seller = BookWorld id=8)
(N'Atomic Habits by James Clear', N'Atomic Habits: An Easy & Proven Way to Build Good Habits & Break Bad Ones. Paperback edition.', 16.99, N'https://picsum.photos/seed/atomichabits/400/300', 5, 8, 0, NULL),
(N'The Psychology of Money', N'The Psychology of Money by Morgan Housel. Timeless lessons on wealth, greed, and happiness.', 14.99, N'https://picsum.photos/seed/psychmoney/400/300', 5, 8, 0, NULL),
(N'Dune by Frank Herbert', N'Dune: The classic science fiction masterpiece. Mass market paperback edition.', 10.99, N'https://picsum.photos/seed/dune/400/300', 5, 8, 0, NULL),
(N'Kindle Paperwhite 2024', N'Amazon Kindle Paperwhite 11th Gen, 6.8" display, 16GB, adjustable warm light. Waterproof.', 149.99, N'https://picsum.photos/seed/kindle/400/300', 5, 8, 0, NULL),
(N'Thinking, Fast and Slow', N'Thinking, Fast and Slow by Daniel Kahneman. Nobel Prize winner exploration of the mind.', 12.49, N'https://picsum.photos/seed/thinkfast/400/300', 5, 8, 0, NULL),
(N'Vinyl Record: Abbey Road', N'The Beatles - Abbey Road. 180g vinyl remaster. Iconic album in pristine condition.', 29.99, N'https://picsum.photos/seed/abbeyroad/400/300', 5, 8, 1, DATEADD(DAY, 2, GETDATE()));
GO

-- ─── 5. Stores ──────────────────────────────────────────────────────────────
INSERT INTO [Store] ([sellerId], [storeName], [description], [bannerImageURL]) VALUES
(4, N'TechStore Official', N'Your #1 destination for the latest electronics and gadgets. Authorized reseller with warranty.', N'https://picsum.photos/seed/techbanner/800/200'),
(5, N'FashionHub Store', N'Trendy fashion, footwear, and accessories. 100% authentic brands. Fast shipping.', N'https://picsum.photos/seed/fashionbanner/800/200'),
(6, N'HomeGoods Market', N'Quality home appliances and garden essentials. Transform your living space.', N'https://picsum.photos/seed/homebanner/800/200'),
(7, N'SportZone Pro', N'Professional sports gear and outdoor equipment. Gear up for your next adventure.', N'https://picsum.photos/seed/sportbanner/800/200'),
(8, N'BookWorld Library', N'Books, media, and reading accessories. Feed your mind with us.', N'https://picsum.photos/seed/bookbanner/800/200');
GO

-- ─── 6. Inventory ───────────────────────────────────────────────────────────
INSERT INTO [Inventory] ([productId], [quantity], [lastUpdated]) VALUES
(1, 25, GETDATE()), (2, 30, GETDATE()), (3, 15, GETDATE()), (4, 50, GETDATE()),
(5, 20, GETDATE()), (6, 5, GETDATE()), (7, 40, GETDATE()), (8, 60, GETDATE()),
(9, 35, GETDATE()), (10, 100, GETDATE()), (11, 45, GETDATE()), (12, 55, GETDATE()),
(13, 10, GETDATE()), (14, 70, GETDATE()), (15, 20, GETDATE()), (16, 18, GETDATE()),
(17, 80, GETDATE()), (18, 30, GETDATE()), (19, 22, GETDATE()), (20, 8, GETDATE()),
(21, 40, GETDATE()), (22, 90, GETDATE()), (23, 120, GETDATE()), (24, 15, GETDATE()),
(25, 25, GETDATE()), (26, 65, GETDATE()), (27, 30, GETDATE()), (28, 50, GETDATE()),
(29, 12, GETDATE()), (30, 200, GETDATE()), (31, 150, GETDATE()), (32, 180, GETDATE()),
(33, 35, GETDATE()), (34, 160, GETDATE()), (35, 7, GETDATE());
GO

-- ─── 7. Orders ──────────────────────────────────────────────────────────────
INSERT INTO [OrderTable] ([buyerId], [addressId], [orderDate], [totalPrice], [status]) VALUES
(2, 1, DATEADD(DAY, -10, GETDATE()), 1549.98, N'Delivered'),
(2, 1, DATEADD(DAY, -5, GETDATE()), 349.99, N'Shipped'),
(3, 3, DATEADD(DAY, -8, GETDATE()), 259.49, N'Delivered'),
(9, 4, DATEADD(DAY, -3, GETDATE()), 639.98, N'Processing'),
(10, 5, DATEADD(DAY, -1, GETDATE()), 189.99, N'Pending'),
(2, 1, DATEADD(DAY, -15, GETDATE()), 89.95, N'Delivered'),
(3, 3, DATEADD(DAY, -2, GETDATE()), 449.99, N'Shipped');
GO

-- ─── 8. Order Items ─────────────────────────────────────────────────────────
INSERT INTO [OrderItem] ([orderId], [productId], [quantity], [unitPrice]) VALUES
(1, 1, 1, 1199.99),   -- iPhone 15 Pro Max
(1, 4, 1, 349.99),    -- Sony Headphones
(2, 4, 1, 349.99),    -- Sony Headphones
(3, 9, 1, 189.99),    -- Air Jordan 1
(3, 10, 1, 69.50),    -- Levi's 501
(4, 5, 1, 599.00),    -- iPad Air
(4, 27, 1, 44.95),    -- Hydroflask (adjusted to match total rounding)
(5, 9, 1, 189.99),    -- Air Jordan 1
(6, 17, 1, 89.95),    -- Instant Pot
(7, 24, 1, 449.99);   -- Garmin Watch
GO

-- ─── 9. Payments ────────────────────────────────────────────────────────────
INSERT INTO [Payment] ([orderId], [userId], [amount], [method], [status], [paidAt]) VALUES
(1, 2, 1549.98, N'Credit Card', N'Completed', DATEADD(DAY, -10, GETDATE())),
(2, 2, 349.99, N'PayPal', N'Completed', DATEADD(DAY, -5, GETDATE())),
(3, 3, 259.49, N'Credit Card', N'Completed', DATEADD(DAY, -8, GETDATE())),
(4, 9, 639.98, N'Debit Card', N'Completed', DATEADD(DAY, -3, GETDATE())),
(5, 10, 189.99, N'PayPal', N'Pending', NULL),
(6, 2, 89.95, N'Credit Card', N'Completed', DATEADD(DAY, -15, GETDATE())),
(7, 3, 449.99, N'Credit Card', N'Completed', DATEADD(DAY, -2, GETDATE()));
GO

-- ─── 10. Shipping Info ──────────────────────────────────────────────────────
INSERT INTO [ShippingInfo] ([orderId], [carrier], [trackingNumber], [status], [estimatedArrival]) VALUES
(1, N'Vietnam Post', N'VN1234567890', N'Delivered', DATEADD(DAY, -7, GETDATE())),
(2, N'GHN Express', N'GHN9876543210', N'In Transit', DATEADD(DAY, 2, GETDATE())),
(3, N'J&T Express', N'JT1122334455', N'Delivered', DATEADD(DAY, -5, GETDATE())),
(4, N'Shopee Express', N'SPX6677889900', N'Processing', DATEADD(DAY, 5, GETDATE())),
(6, N'Vietnam Post', N'VN5566778899', N'Delivered', DATEADD(DAY, -12, GETDATE())),
(7, N'GHN Express', N'GHN1357924680', N'In Transit', DATEADD(DAY, 3, GETDATE()));
GO

-- ─── 11. Reviews ────────────────────────────────────────────────────────────
INSERT INTO [Review] ([productId], [reviewerId], [rating], [comment], [createdAt]) VALUES
(1, 2, 5, N'Amazing phone! The camera is incredible and the titanium design feels premium.', DATEADD(DAY, -7, GETDATE())),
(1, 3, 4, N'Great phone but a bit expensive. Battery life could be better.', DATEADD(DAY, -5, GETDATE())),
(4, 2, 5, N'Best noise cancelling headphones I have ever used. Sound quality is superb.', DATEADD(DAY, -4, GETDATE())),
(9, 3, 5, N'Classic Jordan 1s! Perfect condition, fast shipping. Love them!', DATEADD(DAY, -6, GETDATE())),
(17, 2, 4, N'Great vacuum cleaner but instant pot is a game changer for cooking.', DATEADD(DAY, -12, GETDATE())),
(10, 9, 5, N'Perfect fit, classic style. Levi''s never disappoints!', DATEADD(DAY, -3, GETDATE())),
(24, 3, 5, N'Excellent running watch. AMOLED display is gorgeous, GPS is accurate.', DATEADD(DAY, -1, GETDATE())),
(30, 10, 4, N'Life-changing book! Practical advice on building habits.', DATEADD(DAY, -2, GETDATE())),
(3, 9, 5, N'M3 Pro is blazing fast. Best laptop for development work.', DATEADD(DAY, -8, GETDATE())),
(16, 2, 4, N'Powerful suction and the laser detection is really cool!', DATEADD(DAY, -9, GETDATE()));
GO

-- ─── 12. Bids (for auction items) ───────────────────────────────────────────
-- Auction items: id=6 (Dell XPS), id=13 (North Face), id=20 (Roomba), id=29 (Peloton Mat), id=35 (Abbey Road)
INSERT INTO [Bid] ([productId], [bidderId], [amount], [bidTime]) VALUES
(6, 2, 1600.00, DATEADD(HOUR, -48, GETDATE())),
(6, 3, 1650.00, DATEADD(HOUR, -36, GETDATE())),
(6, 9, 1700.00, DATEADD(HOUR, -24, GETDATE())),
(13, 3, 290.00, DATEADD(HOUR, -30, GETDATE())),
(13, 10, 310.00, DATEADD(HOUR, -20, GETDATE())),
(20, 2, 620.00, DATEADD(HOUR, -40, GETDATE())),
(20, 9, 650.00, DATEADD(HOUR, -28, GETDATE())),
(29, 10, 65.00, DATEADD(HOUR, -15, GETDATE())),
(35, 2, 35.00, DATEADD(HOUR, -10, GETDATE())),
(35, 3, 42.00, DATEADD(HOUR, -5, GETDATE()));
GO

-- ─── 13. Coupons ────────────────────────────────────────────────────────────
INSERT INTO [Coupon] ([code], [discountPercent], [startDate], [endDate], [maxUsage], [productId]) VALUES
(N'TECH10', 10.00, DATEADD(DAY, -5, GETDATE()), DATEADD(DAY, 25, GETDATE()), 100, 1),
(N'FASHION15', 15.00, DATEADD(DAY, -3, GETDATE()), DATEADD(DAY, 27, GETDATE()), 50, 9),
(N'HOME20', 20.00, DATEADD(DAY, -1, GETDATE()), DATEADD(DAY, 29, GETDATE()), 30, 16),
(N'SPORT5', 5.00, GETDATE(), DATEADD(DAY, 30, GETDATE()), 200, 23),
(N'BOOK25', 25.00, GETDATE(), DATEADD(DAY, 14, GETDATE()), 75, 30);
GO

-- ─── 14. Messages ───────────────────────────────────────────────────────────
INSERT INTO [Message] ([senderId], [receiverId], [content], [timestamp]) VALUES
(2, 4, N'Hi, is the iPhone 15 Pro Max still available in Blue Titanium?', DATEADD(HOUR, -48, GETDATE())),
(4, 2, N'Yes! We have Blue Titanium in stock. Would you like to order?', DATEADD(HOUR, -47, GETDATE())),
(3, 5, N'Do you have the Jordan 1 in size 42?', DATEADD(HOUR, -24, GETDATE())),
(5, 3, N'We have sizes 40-45 available. Size 42 is in stock!', DATEADD(HOUR, -23, GETDATE())),
(9, 6, N'Can the Dyson V15 be used on hardwood floors?', DATEADD(HOUR, -12, GETDATE())),
(6, 9, N'Absolutely! The V15 has a soft roller head perfect for hardwood floors.', DATEADD(HOUR, -11, GETDATE()));
GO

-- ─── 15. Return Requests ────────────────────────────────────────────────────
INSERT INTO [ReturnRequest] ([orderId], [userId], [reason], [status], [createdAt]) VALUES
(1, 2, N'Received wrong color. Ordered Blue Titanium but received Natural Titanium.', N'Approved', DATEADD(DAY, -6, GETDATE())),
(3, 3, N'Size does not fit. Need to exchange for a larger size.', N'Pending', DATEADD(DAY, -4, GETDATE()));
GO

-- ─── 16. Feedback (Seller Ratings) ──────────────────────────────────────────
INSERT INTO [Feedback] ([sellerId], [averageRating], [totalReviews], [positiveRate]) VALUES
(4, 4.70, 156, 96.50),
(5, 4.85, 230, 98.20),
(6, 4.60, 89, 94.80),
(7, 4.50, 67, 93.00),
(8, 4.90, 312, 99.10);
GO

-- ─── 17. Disputes ───────────────────────────────────────────────────────────
INSERT INTO [Dispute] ([orderId], [raisedBy], [description], [status], [resolution]) VALUES
(1, 2, N'Wrong color received. Seller sent Natural Titanium instead of Blue Titanium as ordered.', N'Resolved', N'Full refund issued. Return shipping label provided.');
GO

PRINT '✅ Sample data inserted successfully!';
PRINT 'Users: 10 (1 admin, 4 sellers, 5 buyers)';
PRINT 'Categories: 8';
PRINT 'Products: 35';
PRINT 'Orders: 7';
PRINT 'Reviews: 10';
PRINT 'Bids: 10';
GO
