﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NetPro;
using XXX.Plugin.MongoDB.Service;

namespace XXX.Plugin.MongoDB
{
    /// <summary>
    /// Redis 操作示例
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class MongoDBDemoController : ControllerBase
    {
        private readonly ILogger<MongoDBDemoController> _logger;
        private readonly IWebHelper _webHelper;
        private readonly IMongoDBService _mongoDBService;

        public MongoDBDemoController(ILogger<MongoDBDemoController> logger,
            IWebHelper webHelper,
            IMongoDBService mongoDBService)
        {
            _logger = logger;
            _webHelper = webHelper;
            _mongoDBService = mongoDBService;
        }

        /// <summary>
        /// 通过数据库别名key新增到指定数据库
        /// </summary>
        /// <param name="key">数据库别名key</param>
        /// <returns></returns>
        [HttpGet("InsertOne")]
        [ProducesResponseType(200)]
        public IActionResult InsertOne(string key)
        {
            _mongoDBService.InsertOne(key);
            return Ok();
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="key">数据库别名key</param>
        /// <returns></returns>
        [HttpGet("DeleteOne")]
        [ProducesResponseType(200)]
        public IActionResult DeleteOne(string key)
        {
            _mongoDBService.DeleteOne(key);
            return Ok();
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="key">数据库别名key</param>
        /// <returns></returns>
        [HttpGet("ReplaceOne")]
        [ProducesResponseType(200)]
        public IActionResult ReplaceOne(string key)
        {
            _mongoDBService.ReplaceOne(key);
            return Ok();
        }

        /// <summary>
        /// 查找
        /// </summary>
        /// <param name="key">数据库别名key</param>
        /// <returns></returns>
        [HttpGet("Find")]
        [ProducesResponseType(200)]
        public IActionResult Find(string key)
        {
            _mongoDBService.Find(key);
            return Ok();
        }
    }
}
